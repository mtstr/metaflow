namespace Metaflow.Orleans

open Metaflow
open FSharp.Control.Tasks
open FSharp.UMX

module FeatureExec =
    

    let execute call handler eventStore logger =
        task {
            let! featureOutput = handler.Handler(call.Input)

            let! saveResult =
                EventSourcing.saveEvent eventStore %($"agg:{call.AggregateRootId}") featureOutput call.Feature logger

            let result =
                match (featureOutput, saveResult) with
                | (Done m, Result.Ok _) -> Ok m
                | (_, Result.Error ex) -> Error(ServerError ex)
                | (Rejected r, Result.Ok _) -> Error(RequestError r)
                | (Exception ex, Result.Ok _) -> Error(ServerError ex)
                | (Ignored _, Result.Ok _) -> Ok None

            return result
        }

