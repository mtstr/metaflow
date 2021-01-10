namespace Metaflow

open System
open System.Threading.Tasks
open Metaflow
open Orleans
open FSharp.Control.Tasks
open Orleans.Concurrency
open Serilog

type IFeatureClientGrain =
    inherit IGrainWithStringKey
    abstract Delete<'model> : string * bool -> Task<Result<unit, WorkflowFailure>>

type FeatureClient(clusterClient: IClusterClient) =
    member this.Delete<'model>(aggregateRootId: string, awaitState: bool) =
        let grain =
            clusterClient.GetGrain<IFeatureClientGrain>(Guid.NewGuid().ToString())

        grain.Delete<'model>(aggregateRootId, awaitState)

[<StatelessWorker>]
type FeatureClientGrain(clusterClient: IClusterClient, workflows: Workflow seq) =
    inherit Grain()

    let workflowMap =
        workflows
        |> Seq.map (fun f -> ((f.Feature.Model.FullName, f.Feature.Operation), f))
        |> Map.ofSeq

    interface IFeatureClientGrain with
        member this.Delete<'model>(aggregateRootId: string, awaitState: bool) =
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
                                Workflows.run<unit, Delete, 'model>
                                    workflow
                                    call
                                    clusterClient
                                    { RequestId = Guid.NewGuid().ToString() }
                                    Log.Logger
                        }
            }
