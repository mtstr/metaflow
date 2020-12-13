namespace Metaflow

open Metaflow
open Orleans
open System.Threading.Tasks
open FSharp.Control.Tasks


    type IAggregateGrain =
        inherit IGrainWithStringKey
        abstract Call: FeatureCall -> Task<FeatureResult>

    type EventDto<'e> = { Name: string; Event: 'e }

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
