namespace Metaflow.Orleans

open Metaflow
open FSharp.Control.Tasks
open Metaflow.Orleans
open Metaflow.Workflows
open FSharp.Control

module Workflows =
    let executeFeature<'op, 'model, 'input> (call: FeatureCall<'input>) (clusterClient: Orleans.IClusterClient) =
        task {

            let featureGrain =
                clusterClient.GetGrain<IFeatureGrain<'op, 'model, 'input>>(call.Id)

            let! result = featureGrain.Call(call)

            return result
        }
    let apply<'model> (step: Step)
                      (id: string)
                      rc
                      (mu: Result<'model option, FeatureFailure>)
                      (clusterClient: Orleans.IClusterClient)
                      logger
                      =
        async {
            let f () =
                task {
                    let grainType =
                        typedefof<IStepGrain<_, _>>
                            .MakeGenericType(typeof<'model>, step.Handler)

                    let grain =
                        clusterClient.GetGrain(grainType, id) :?> IStepGrain<'model>

                    let! result = grain.Call(rc, mu)

                    logResult result step.Name step.Workflow rc logger

                    return result
                }


            if step.Background then
                f ()
                |> Async.AwaitTask
                |> Async.Ignore
                |> Async.Start

                return None
            else
                let! r = f () |> Async.AwaitTask
                return Some r
        }

    let private _apply<'m> id (requestContext: RequestContext) mutation clusterClient (steps: Step list) logger =
        asyncSeq {
            for step in steps do
                let! r = apply<'m> step id requestContext mutation clusterClient logger
                if r.IsSome then yield r.Value
        }



    let run<'input, 'op, 'model> workflow
                                 (call: FeatureCall<'input>)
                                 (clusterClient: Orleans.IClusterClient)
                                 requestContext
                                 logger
                                 =
        task {

            let! featureResult =
                match call.Feature.ConcurrencyScope with
                | Feature -> task { return! executeFeature<'op, 'model, 'input> call clusterClient }
                | _ ->
                    task {
                        let grain =
                            clusterClient.GetGrain<IConcurrencyScopeGrain>($"{call.ModelId}")

                        return! grain.Execute<'op, 'model, 'input>(call)
                    }


            let stepResults =
                _apply<'model> call.ModelId requestContext featureResult clusterClient workflow.Steps logger

            let! stepError =
                stepResults
                |> AsyncSeq.tryFind (fun sr -> not (sr.Success))

            let result =
                match (featureResult, stepError) with
                | (Ok _, Some (StepResult.Failed f)) -> Error(WorkflowFailure.StepFailure f)
                | (Error e, _) -> Error(FeatureFailure e)
                | (_, _) -> Ok()


            return result

        }
