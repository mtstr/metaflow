namespace Metaflow

open Metaflow
open Serilog
open FSharp.Control

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
