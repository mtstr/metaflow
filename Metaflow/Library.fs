namespace Metaflow

open System.Text.Json.Serialization
open System.Text.Json
open System.Collections.Generic
open System.Reflection
open Microsoft.AspNetCore.Http

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
        options.PropertyNameCaseInsensitive <- true
        options.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
        options



module EventSourcing =
    type EventDef = { Feature: string }

    let private tryResolveType (eventName: string) (eventJson: string) (features: Map<string, Feature>) =
        let (resource, version) =
            match List.ofArray (eventName.Split(":")) with
            | [ _; r; v ] -> (r, v |> int)
            | [ _; r; v; _ ] -> (r, v |> int)
            | _ -> failwith "Unexpected event name. Are you sure?"

        let eventDef =
            System.Text.Json.JsonSerializer.Deserialize<EventDef>(eventJson, Json.options)

        let maybeFeature =
            features |> Map.tryFind (eventDef.Feature)

        match maybeFeature with
        | Some f when f.RequiredState.IsSome
                      && f.RequiredState.Value.Name = resource -> f.RequiredState
        | Some f -> Some f.Model
        | _ -> None

    let deserialize (eventName: string) (eventJson: string) (features: Map<string, Feature>) =

        let maybeType =
            tryResolveType eventName eventJson features

        let eventType =
            match maybeType with
            | Some t -> Some(typedefof<Metaflow.Event<_>>.MakeGenericType(t))
            | None -> None

        match eventType with
        | Some t -> Some(System.Text.Json.JsonSerializer.Deserialize(eventJson, t, Json.options))
        | None -> None

    type IEventStreamId<'T> = { Get: string -> string }

    type IEventSerializer =
        abstract Deserialize: string -> string -> obj option

    type EventSerializer(features: IDictionary<string, Feature>) =
        interface IEventSerializer with
            member __.Deserialize (eventName: string) (eventJson: string) =
                let featureMap =
                    List.ofSeq features
                    |> List.map (fun kv -> (kv.Key, kv.Value))
                    |> Map.ofList

                deserialize eventName eventJson featureMap
