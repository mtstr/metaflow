namespace Metaflow

open Orleans
open System.Threading.Tasks
open FSharp.Control.Tasks
open Microsoft.Extensions.Logging
open EventStore.Client
open System.Text
open System

module Grains =
    type IAggregateGrain =
        inherit IGrainWithStringKey
        abstract Call: FeatureCall -> Task<FeatureResult>

    type IFeatureGrain =
        inherit IGrainWithStringKey
        abstract Call: EventStream * FeatureCall -> Task<FeatureResult>

    type IFeatureGrain<'state, 'di> =
        inherit IFeatureGrain

    type EventDto<'e> = { Name: string; Event: 'e }

    type FeatureGrain<'model, 'state, 'di>(clusterClient: IClusterClient,
                                           eventStore: EventStoreClient,
                                           logger: ILogger<FeatureGrain<'model, 'state, 'di>>,
                                           handler: FeatureHandler<'model, 'state, 'di>,
                                           service: IFeatureService<'di>) =
        inherit Grain()

        member private this.SaveEvent(stream: string, event: Event<'model>, expectedversion: int) =

            task {
                try

                    let jsonBytes =
                        Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(event, Json.options))
                        |> ReadOnlyMemory<byte>

                    let eventData =
                        EventData(Uuid.NewUuid(), event.Name(), jsonBytes)

                    let streamRevision =
                        if expectedversion = 0 then
                            StreamRevision.None
                        else
                            Convert.ToUInt64(expectedversion - 1)
                            |> StreamRevision

                    let! r = eventStore.AppendToStreamAsync(stream, streamRevision, [ eventData ])

                    r |> ignore

                with ex -> logger.LogCritical(50003 |> EventId, ex, ex.Message)

                return true
            }

        interface IFeatureGrain<'state, 'di> with
            member this.Call(stream: EventStream, call: FeatureCall) =
                task {
                    let! featureOutput =
                        match (handler, call.Args.Value, service.Get()) with
                        | (AggregateRootWithDI h, AggregateRoot id, Some s) -> task { return h id s }
                        | _ -> task { return Ignore "" }

                    let event =
                        Metaflow.Event<'model>
                            .FromOutput(featureOutput, (call.Feature))

                    let! saveResult = this.SaveEvent(stream.Name, event, stream.Version)

                    let result =
                        match (featureOutput, saveResult) with
                        | (Success _, true) -> Ok
                        | (_, false) -> ServerError "Failed to save event"
                        | (Reject r, true) -> RequestError r
                        | (Failure f, true) -> ServerError f
                        | (Ignore _, true) -> Ok

                    return result
                }

    type AggregateGrain(clusterClient: IClusterClient) =
        inherit Grain()

        interface IAggregateGrain with
            member this.Call(call: FeatureCall) =
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
                        clusterClient.GetGrain(featureGrainType, $"ftr:{call.Feature.Name}:{call.Args.Value}")
                        :?> IFeatureGrain

                    let stream = { Name = "store"; Version = 0 }
                    let! result = featureGrain.Call(stream, call)
                    return result
                }
