namespace Metaflow

open Metaflow
open Orleans
open System.Threading.Tasks
open FSharp.Control.Tasks


type IConcurrencyScopeGrain =
    inherit IGrainWithStringKey
    abstract Execute<'op, 'model, 'input> : FeatureCall<'input> -> Task<FeatureResult>

type ConcurrencyScopeGrain(clusterClient: IClusterClient) =
    inherit Grain()

    interface IConcurrencyScopeGrain with
        member this.Execute<'op, 'model, 'input>(call: FeatureCall<'input>) =
            task { return! FeatureHelper.execute<'op, 'model, 'input> call clusterClient }
