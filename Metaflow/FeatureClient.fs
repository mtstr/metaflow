namespace Metaflow

open System
open Metaflow
open Orleans
open FSharp.Control.Tasks
open Serilog

type FeatureClient(clusterClient: IClusterClient, workflows: Workflow seq) =
    let workflowMap =
        workflows
        |> Seq.map (fun f -> ((f.Feature.Model.FullName, f.Feature.Operation), f))
        |> Map.ofSeq

    member this.Delete<'model>(aggregateRootId: string, awaitState: bool) =
        task {
            let m = workflowMap
            let workflowOption =
                m
                |> Map.tryFind (typeof<'model>.FullName, Operation.DELETE)

            return!
                match workflowOption with
                | None ->
                    task {
                        return
                            { FeatureResult = NotFound
                              StepError = None }
                    }
                | Some workflow ->
                    let call =
                        { Feature = workflow.Feature
                          Input = Id(OwnedValueId { AggregateRootId = aggregateRootId })
                          AwaitState = awaitState }

                    task {
                        return!
                            Workflows.run<unit, Delete, 'model>
                                workflow
                                call
                                clusterClient
                                { RequestId = Guid.NewGuid().ToString() }
                                Log.Logger
                    }
        }
