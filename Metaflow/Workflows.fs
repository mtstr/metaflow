namespace Metaflow

open System
open System.Threading.Tasks
open Metaflow
open Orleans
open Serilog
open FSharp.Control.Tasks
open FSharp.Control

module Workflows =
    type RequestContext = { RequestId: string }

    type StepResult =
        | Done
        | Skipped
        | Error of string
        | Exception of Exception
        member this.Success =
            match this with
            | Done -> true
            | _ -> false

    type IStepGrain<'handler> =
        inherit IGrainWithStringKey
        abstract Call<'model> : (RequestContext * FeatureResult<'model>) -> Task<StepResult>

    type WorkflowResult<'model> =
        { FeatureResult: FeatureResult<'model>
          StepError: StepResult option }

    let logResult result step wf rc (logger: ILogger) =
        let inf =
            "Performed {Step} as part of {Workflow}. Request: {RequestId}"

        let exc =
            "Exception in {Step} during {Workflow}. Request: {RequestId}"

        let wrn =
            "Skipped {Step} during {Workflow}. Request: {RequestId}"

        let err e =
            sprintf "Error {Step} during {Workflow}: %s. Request: {RequestId}" e

        match result with
        | Done -> logger.Information(inf, step, wf, rc.RequestId)
        | Exception ex -> logger.Error(ex, exc, step, wf, rc.RequestId)
        | Skipped -> logger.Warning(wrn, step, wf, rc.RequestId)
        | Error e -> logger.Error(err e, step, wf, rc.RequestId)


    let apply (step: Step) (id: string) rc mu (clusterClient: IClusterClient) logger =
        async {
            let f () =
                task {
                    let grainType =
                        typeof<IStepGrain<_>>
                            .MakeGenericType(step.Handler)

                    let grain =
                        clusterClient.GetGrain(grainType, id) :?> IStepGrain<_>

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

    let private _apply id (requestContext: RequestContext) mutation clusterClient (steps: Step list) logger =
        asyncSeq {
            for step in steps do
                let! r = apply step id requestContext mutation clusterClient logger
                if r.IsSome then yield r.Value
        }



    let run<'input, 'op, 'model> workflow
                                 (call: FeatureCall<'input>)
                                 (clusterClient: IClusterClient)
                                 requestContext
                                 logger
                                 =
        task {

            let! featureResult =
                match call.Feature.ConcurrencyScope with
                | Feature -> task { return! Features.execute<'op, 'model, 'input> call clusterClient }
                | _ ->
                    task {
                        let grain =
                            clusterClient.GetGrain<IConcurrencyScopeGrain>($"{call.ModelId}")

                        return! grain.Execute<'op, 'model, 'input>(call)
                    }


            let stepResults =
                _apply call.ModelId requestContext featureResult clusterClient workflow.Steps logger

            let! stepError =
                stepResults
                |> AsyncSeq.tryFind (fun sr -> not (sr.Success))

            let result =
                { FeatureResult = featureResult
                  StepError = stepError }

            return result

        }

    

    let private g f =
        async {
            let! result = f

            return
                match result with
                | Result.Ok _ -> StepResult.Done
                | Result.Error e -> StepResult.Error e
        }

    let tryIfSuccess m f =
        async {
            let h =
                match m with
                | FeatureResult.Ok a -> g (f a)
                | _ -> async { return StepResult.Skipped }

            let! result = Async.Catch h

            return
                match result with
                | Choice1Of2 sr -> sr
                | Choice2Of2 ex -> StepResult.Exception ex
        }
