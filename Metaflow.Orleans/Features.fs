namespace Metaflow.Orleans

open Metaflow
open FSharp.Control.Tasks
open FSharp.UMX

module FeatureExec =


    let execute (call: FeatureCall<'model>) (prerequisite: IRequires<'model>) eventStore logger =
        task {
            let! featureOutput =
                try
                    async {
                        let! met = prerequisite.Check(call.Input)

                        return
                            match met with
                            | true -> Done
                            | false -> Rejected
                    }
                with ex -> async { return Exception ex }


            let! saveResult =
                EventSourcing.saveEvent eventStore %($"agg:{call.AggregateRootId}") featureOutput call.Feature logger

            let result =
                match (featureOutput, saveResult) with
                | (Done, Result.Ok _) -> Ok()
                | (_, Result.Error ex) -> Error(ServerError ex)
                | (Rejected, Result.Ok _) -> Error(RequestError "Prerequisite not met")
                | (Exception ex, Result.Ok _) -> Error(ServerError ex)

            return result
        }
