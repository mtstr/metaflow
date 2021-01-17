namespace Metaflow

open System
open System.Text
open System.Text.Json
open EventStore.Client
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open FSharp.Control.Tasks
open FSharp.UMX

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


    let saveEvent (eventStore: EventStoreClient) (stream: string<eventStream>) (output: FeatureOutput<'op, 'model>) (feature: Feature) (logger: ILogger) =

        task {
            try
                let event =
                    {| output = output
                       operation = typeof<'op>.Name
                       model = typeof<'model>.Name
                       feature = feature.Name
                       version = feature.Version |}

                let jsonBytes =
                    Encoding.UTF8.GetBytes(JsonSerializer.Serialize(event, Json.options))
                    |> ReadOnlyMemory<byte>

                let eventData =
                    EventData(Uuid.NewUuid(), output.Name(feature), jsonBytes)

                let! _ = eventStore.AppendToStreamAsync(stream.ToString(), StreamState.Any, [ eventData ])

                return (Result.Ok())
            with ex ->
                logger.LogCritical(50003 |> EventId, ex, ex.Message)
                return Result.Error ex
        }