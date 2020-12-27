namespace Metaflow

open EventStore.ClientAPI
open Metaflow
open Orleans
open System.Threading.Tasks
open FSharp.Control.Tasks
open Microsoft.Extensions.Logging
open EventStore.Client
open System.Text
open System
open FSharp.UMX

type IFeatureGrain<'op, 'model, 'input> =
    inherit IGrainWithStringKey
    abstract Call: FeatureCall<'input> -> Task<FeatureResult>

type FeatureGrain<'op, 'model, 'input>(eventStore: EventStoreClient,
                                       clusterClient: IClusterClient,
                                       logger: ILogger<FeatureGrain<'input, 'model, 'op>>,
                                       handler: FeatureHandler<'op, 'model, 'input>,
                                       stateTriggers: StateTrigger<'model> seq) =
    inherit Grain()

    member private this.SaveEvent(stream: string<eventStream>, event: FeatureOutput<'op, 'model>, feature: Feature) =

        task {
            try

                let jsonBytes =
                    Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(event, Json.options))
                    |> ReadOnlyMemory<byte>

                let eventData =
                    EventData(Uuid.NewUuid(), event.Name(feature), jsonBytes)

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
                    | (Succeeded _, Result.Ok _) -> Ok
                    | (_, Result.Error ex) -> ServerError ex
                    | (Rejected r, Result.Ok s) -> RequestError r
                    | (Failed ex, Result.Ok s) -> ServerError ex
                    | (Ignored _, Result.Ok _) -> Ok


                //                let ts =
//                    match featureOutput with
//                    | Succeeded m ->
//                        stateTriggers
//                        |> Seq.map (fun o ->
//                            clusterClient
//                                .GetGrain<IStateGrain<'model>>(o.StateIdResolver(call.ModelId, m))
//                                .Call(m)
//                            |> Async.AwaitTask)
//                        |> List.ofSeq
//                    | None -> []
//
//                ts |> List.iter Async.Start

                return result
            }

module FeatureHelper =
    let execute<'op, 'model, 'input> (call: FeatureCall<'input>) (clusterClient: IClusterClient) =
        task {

            let featureGrain =
                clusterClient.GetGrain<IFeatureGrain<'op, 'model, 'input>>(call.Id)

            let! result = featureGrain.Call(call)

            return result
        }

    let deleteValue<'m> (aggregate: string) (ver: int): FeatureHandler<Delete, 'm, unit> =
        let f =
            { Operation = Operation.DELETE
              ConcurrencyScope = ConcurrencyScope.Aggregate
              Model = typeof<'m>
              ModelKind = ModelKind.OwnedValue aggregate
              RequiredService = None
              RequiredState = None
              Version = ver }

        let h =
            fun _ -> async { return FeatureOutput<Delete, 'm>.Succeeded None }

        { Feature = f; Handler = h }
