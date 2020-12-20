namespace Metaflow

open Metaflow
open Orleans
open System.Threading.Tasks
open FSharp.Control.Tasks


type IAggregateGrain =
    inherit IGrainWithStringKey
    abstract Execute: FeatureCall -> Task<FeatureResult>

type EventDto<'e> = { Name: string; Event: 'e }

type AggregateGrain(clusterClient: IClusterClient) =
    inherit Grain()

    interface IAggregateGrain with
        member this.Execute(call: FeatureCall) =
            task {
                return! Features.execute call clusterClient
            }
