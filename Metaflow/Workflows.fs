namespace Metaflow

open System.Threading.Tasks
open Metaflow
open Orleans
open Serilog
open FSharp.Control.Tasks
open FSharp.Control


type IStepHandler<'model> =
    abstract Call: RequestContext * Result<'model option, FeatureFailure> -> Task<StepResult>

type IStepGrain<'model> =
    inherit IGrainWithStringKey
    abstract Call: RequestContext * Result<'model option, FeatureFailure> -> Task<StepResult>

type IStepGrain<'model, 'handler when 'handler :> IStepHandler<'model>> =
    inherit IStepGrain<'model>

type StepGrain<'model, 'handler when 'handler :> IStepHandler<'model>>(handler: 'handler) =
    inherit Grain()

    interface IStepGrain<'model, 'handler> with
        member this.Call(rc, result) =
            task { return! handler.Call(rc, result) }

module Workflows =
    let logResult (result: StepResult) step wf rc (logger: ILogger) =
        let inf =
            "Performed {Step} as part of {Workflow}. Request: {RequestId}"

        let exc =
            "Exception in {Step} during {Workflow}. Request: {RequestId}"

        let wrn =
            "Skipped {Step} during {Workflow}. Request: {RequestId}"

        let err e =
            sprintf "Error {Step} during {Workflow}: %s. Request: {RequestId}" e

        match result with
        | StepResult.Done -> logger.Information(inf, step, wf, rc.RequestId)
        | Skipped -> logger.Warning(wrn, step, wf, rc.RequestId)
        | Failed f ->
            match f with
            | StepFailure.Exception ex -> logger.Error(ex, exc, step, wf, rc.RequestId)
            | InvalidOperation e -> logger.Error(err e, step, wf, rc.RequestId)


    let apply<'model> (step: Step)
                      (id: string)
                      rc
                      (mu: Result<'model option, FeatureFailure>)
                      (clusterClient: IClusterClient)
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



    let private g f =
        async {
            let! result = f

            return
                match result with
                | Result.Ok _ -> StepResult.Done
                | Result.Error e -> StepResult.Failed(StepFailure.InvalidOperation e)
        }

    let tryIfSuccess m f =
        async {
            let h =
                match m with
                | Result.Ok a -> g (f a)
                | _ -> async { return StepResult.Skipped }

            let! result = Async.Catch h

            return
                match result with
                | Choice1Of2 sr -> sr
                | Choice2Of2 ex -> StepResult.Failed(StepFailure.Exception ex)
        }
