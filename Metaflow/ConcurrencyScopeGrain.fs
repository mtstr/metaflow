namespace Metaflow

open Metaflow
open Orleans
open FSharp.Control.Tasks



type ConcurrencyScopeGrain(clusterClient: IClusterClient) =
    inherit Grain()

    interface IConcurrencyScopeGrain with
        member this.Execute<'op, 'model, 'input>(call: FeatureCall<'input>) =
            task { return! Features.execute<'op, 'model, 'input> call clusterClient }
