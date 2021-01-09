namespace Metaflow

open Metaflow
open Orleans
open System.Threading.Tasks
open FSharp.Control.Tasks
open Microsoft.Extensions.Logging
open EventStore.Client
open System.Text
open System
open FSharp.UMX


type FeatureGrain<'op, 'model, 'input>(eventStore: EventStoreClient,
                                       logger: ILogger<FeatureGrain<'input, 'model, 'op>>,
                                       handler: FeatureHandler<'op, 'model, 'input>) =
    inherit Grain()

    member private this.SaveEvent(stream: string<eventStream>, output: FeatureOutput<'op, 'model>, feature: Feature) =

        task {
            try
                let event =
                    {| output = output
                       operation = typeof<'op>.Name
                       model = typeof<'model>.Name
                       feature = feature.Name
                       version = feature.Version |}

                let jsonBytes =
                    Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(event, Json.options))
                    |> ReadOnlyMemory<byte>

                let eventData =
                    EventData(Uuid.NewUuid(), output.Name(feature), jsonBytes)

                let! _ = eventStore.AppendToStreamAsync(stream.ToString(), StreamState.Any, [ eventData ])

                return (Result.Ok())
            with ex ->
                logger.LogCritical(50003 |> EventId, ex, ex.Message)
                return Result.Error ex
        }

    interface IFeatureGrain<'op, 'model, 'input> with
        member this.Call(call) =
            task {
                let! featureOutput = handler.Handler(call.Input)

                let! saveResult = this.SaveEvent(%($"agg:{call.AggregateRootId}"), featureOutput, call.Feature)

                let result =
                    match (featureOutput, saveResult) with
                    | (Done m, Result.Ok _) -> Ok m
                    | (_, Result.Error ex) -> ServerError ex
                    | (Rejected r, Result.Ok _) -> RequestError r
                    | (Failed ex, Result.Ok _) -> ServerError ex
                    | (Ignored _, Result.Ok _) -> Ok None

                return result
            }
