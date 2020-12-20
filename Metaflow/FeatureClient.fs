namespace Metaflow

open Metaflow
open Orleans
open FSharp.Control.Tasks

type FeatureClient(clusterClient: IClusterClient, featureResolver: IFeatureResolver) =
    member this.Delete<'t>(aggregateRootId: string, awaitState: bool) =
        task {
            let maybeFeature = featureResolver.Resolve<'t>(DELETE)

            return!
                match maybeFeature with
                | None -> task { return NotFound }
                | Some feature ->
                    let call =
                        { Feature = feature
                          Args =
                              { Definition = feature.ModelKind
                                Value = OwnedValueId { AggregateRootId = aggregateRootId } }
                          AwaitState = awaitState }

                    match (feature.ModelKind, feature.Scoped) with
                    | (OwnedValue _, true) ->
                        task {
                            let grain =
                                clusterClient.GetGrain<IAggregateGrain>($"agg_{aggregateRootId}")

                            return! grain.Execute(call)
                        }
                    | (OwnedValue _, false) -> task { return! Features.execute call clusterClient }

        }
