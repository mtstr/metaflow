namespace Metaflow.Orleans.Features

open Metaflow
open FSharp.Control.Tasks
open System
open Serilog

type Delete<'model> =
    
   static member Execute workflowMap clusterClient (aggregateRootId: string) (awaitState: bool) =
        task {
            let workflowOption =
                workflowMap
                |> Map.tryFind (typeof<'model>.FullName, Operation.DELETE)

            return!
                match workflowOption with
                | None -> task { return Error(WorkflowFailure.FeatureFailure(FeatureFailure.NotFound)) }
                | Some workflow ->
                    let call =
                        { Feature = workflow.Feature
                          Input = Id(OwnedValueId { AggregateRootId = aggregateRootId })
                          AwaitState = awaitState }

                    task {
                        return!
                            Metaflow.Orleans.Workflows.run<unit, Delete, 'model>
                                workflow
                                call
                                clusterClient
                                { RequestId = Guid.NewGuid().ToString() }
                                Log.Logger
                    }
        }
