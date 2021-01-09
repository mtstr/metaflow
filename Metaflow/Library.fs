﻿namespace Metaflow

open System.Text.Json.Serialization
open System.Text.Json
open System.Reflection
open Microsoft.AspNetCore.Http
open Orleans
open FSharp.Control.Tasks

type State<'T>(value: 'T option) =
    new() = State(None)
    member val Value = value with get, set
    member __.Exists = value.IsSome

    member __.Apply(event: Event<'a>) =

        let f t (m: MethodInfo) =
            let p = m.GetParameters() |> List.ofSeq

            m.IsPublic
            && m.Name = "Apply"
            && m.ReturnType = typeof<'T>
            && (p |> List.length) = 1
            && (p |> List.head).ParameterType = t

        let mi =
            typeof<'T>.GetMethods()
            |> List.ofSeq
            |> List.tryFind (f (event.GetType()))

        match mi with
        | Some method -> Some(method.Invoke(value, [| event :> obj |]) :?> 'T)
        | None -> value
module Features =
    let execute<'op, 'model, 'input> (call: FeatureCall<'input>) (clusterClient: IClusterClient) =
        task {

            let featureGrain =
                clusterClient.GetGrain<IFeatureGrain<'op, 'model, 'input>>(call.Id)

            let! result = featureGrain.Call(call)

            return result
        }
module Json =
    let converter =
        JsonFSharpConverter
            (unionTagCaseInsensitive = true,
             unionEncoding =
                 (JsonUnionEncoding.ExternalTag
                  ||| JsonUnionEncoding.NamedFields
                  ||| JsonUnionEncoding.UnwrapFieldlessTags
                  ||| JsonUnionEncoding.UnwrapOption))


    let options =
        let options = JsonSerializerOptions()
        options.Converters.Add(converter)
        options.IgnoreNullValues <- true
        options.PropertyNameCaseInsensitive <- true
        options.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
        options



module EventSourcing =
    type EventDef = { Feature: string }
    type EventData<'op, 'model> = { Data: FeatureOutput<'op, 'model> }

    let private tryResolveTypes (eventJson: string) (features: Map<string, Feature>) =

        let eventDef =
            JsonSerializer.Deserialize<EventDef>(eventJson, Json.options)

        let maybeFeature =
            features |> Map.tryFind (eventDef.Feature)

        match maybeFeature with
        | Some f -> Some(f.Operation.AsType(), f.Model)
        | _ -> None

    let deserialize (eventJson: string) (features: Map<string, Feature>) =

        let maybeTypes = tryResolveTypes eventJson features

        let eventType =
            match maybeTypes with
            | Some (opType, modelType) ->
                Some
                    (typedefof<EventData<_, _>>
                        .MakeGenericType(opType, modelType))
            | None -> None

        match eventType with
        | Some t -> Some((JsonSerializer.Deserialize(eventJson, t, Json.options) :?> EventData<_,_>).Data)
        | None -> None

    type IEventStreamId<'T> = { Get: string -> string }

    type IEventSerializer =
        abstract Deserialize: string -> FeatureOutput<_,_> option

    type EventSerializer(features: Feature seq) =
        let featureMap =
            List.ofSeq features
            |> List.map (fun kv -> (kv.Name, kv))
            |> Map.ofList

        interface IEventSerializer with
            member __.Deserialize(eventJson: string) =

                deserialize eventJson featureMap
