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

type IFeatureGrain =
    inherit IGrainWithStringKey
    abstract Call: FeatureCall -> Task<FeatureResult>

type IFeatureGrain<'state, 'di> =
    inherit IFeatureGrain


type FeatureGrain<'model, 'state, 'di>(eventStore: EventStoreClient,
                                       clusterClient: IClusterClient,
                                       logger: ILogger<FeatureGrain<'model, 'state, 'di>>,
                                       handler: FeatureHandler<'model, 'state, 'di>,
                                       stateObservers: StateObserver<'model> seq,
                                       service: IFeatureService<'di>) =
    inherit Grain()

    member private this.SaveEvent(stream: string<eventStream>, event: Event<'model>) =

        task {
            try

                let jsonBytes =
                    Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(event, Json.options))
                    |> ReadOnlyMemory<byte>

                let eventData =
                    EventData(Uuid.NewUuid(), event.Name(), jsonBytes)

                let streamRevision =
                    Convert.ToUInt64(ExpectedVersion.Any)
                    |> StreamRevision

                let! r = eventStore.AppendToStreamAsync(stream.ToString(), streamRevision, [ eventData ])


                r |> ignore

            with ex -> logger.LogCritical(50003 |> EventId, ex, ex.Message)

            return true
        }

    interface IFeatureGrain<'state, 'di> with
        member this.Call(call: FeatureCall) =
            task {
                let! featureOutput =
                    match (handler, call.Args.Value, service.Get()) with
                    | (AggregateRootWithDI h, AggregateRootId id, Some s) -> task { return h id s }
                    | _ -> task { return Ignore "" }

                let event =
                    Metaflow.Event<'model>
                        .FromOutput(featureOutput, (call.Feature))

                let! saveResult = this.SaveEvent(%call.AggregateRootId, event)


                let (result, modelChangeOption) =
                    match (featureOutput, saveResult) with
                    | (Success m, true) -> (Ok, Some m)
                    | (_, false) -> (ServerError "Failed to save event", None)
                    | (Reject r, true) -> (RequestError r, None)
                    | (Failure f, true) -> (ServerError f, None)
                    | (Ignore _, true) -> (Ok, None)

                let model =
                    match modelChangeOption with
                    | Some { Before = Some b; After = Some a } -> Some a
                    | Some { Before = Some b; After = None } -> Some b
                    | Some { Before = None; After = Some a } -> Some a
                    | _ -> None

                let ts =
                    match model with
                    | Some m ->
                        stateObservers
                        |> Seq.map (fun o ->
                            clusterClient
                                .GetGrain<IStateGrain<'model>>(o.StateIdResolver(m))
                                .Call(modelChangeOption.Value)
                            |> Async.AwaitTask)
                        |> List.ofSeq
                    | None -> []

                ts |> List.iter Async.Start

                return result
            }
module Features =
    let execute call (clusterClient: IClusterClient) =
        task {
            let (stateType, serviceType) =
                match (call.Feature.RequiredState, call.Feature.RequiredService) with
                | (Some s, Some d) -> (s, d)
                | (Some s, None) -> (s, typeof<UnitType>)
                | (None, Some d) -> (typeof<UnitType>, d)
                | (None, None) -> (typeof<UnitType>, typeof<UnitType>)

            let featureGrainType =
                typedefof<IFeatureGrain<_, _>>
                    .MakeGenericType(stateType, serviceType)

            let featureGrain =
                clusterClient.GetGrain(featureGrainType, call.Id) :?> IFeatureGrain

            return! featureGrain.Call(call)
        }