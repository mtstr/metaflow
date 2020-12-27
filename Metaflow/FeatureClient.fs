namespace Metaflow

open Metaflow
open Orleans
open FSharp.Control.Tasks

type FeatureClient(clusterClient: IClusterClient, features: Feature seq) =
    member this.Delete<'model>(aggregateRootId: string, awaitState: bool) =
        task {
            let maybeFeature =
                features
                |> List.ofSeq
                |> List.tryFind (fun f -> f.Model = typeof<'model> && f.Operation = DELETE)

            return!
                match maybeFeature with
                | None -> task { return NotFound }
                | Some feature ->
                    let call =
                        { Feature = feature
                          Input = Id(OwnedValueId { AggregateRootId = aggregateRootId })
                          AwaitState = awaitState }

                    match feature.ConcurrencyScope with
                    | Aggregate ->
                        task {
                            let grain =
                                clusterClient.GetGrain<IConcurrencyScopeGrain>($"agg_{aggregateRootId}")

                            let! result = grain.Execute<Delete, 'model, UnitType>(call)

                            return result
                        }
                    | Entity ->
                        task {
                            let grain =
                                clusterClient.GetGrain<IConcurrencyScopeGrain>($"ent_{aggregateRootId}")

                            return! grain.Execute<Delete, 'model, UnitType>(call)
                        }
                    | Feature -> task { return! FeatureHelper.execute<Delete, 'model, UnitType> call clusterClient }

        }
