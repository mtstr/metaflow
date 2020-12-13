namespace Metaflow

open System.Collections.Generic
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks
open Orleans

module Http =
    let routeFeature (ctx: Microsoft.AspNetCore.Http.HttpContext) =
        { Definition = FeatureArgsDefinition.AggregateRoot "store"
          Value = AggregateRoot { Id = "store_1" } }

    type FeatureRoutingMiddleware(next: RequestDelegate, featureRouting: IDictionary<FeatureArgsDefinition, Feature>) =
        let featureRoutingMap =
            featureRouting
            |> Seq.map (fun kv -> (kv.Key, kv.Value))
            |> Map.ofSeq

        member this.InvokeAsync(context: HttpContext) =
            unitTask {
                let featureArgs = routeFeature context

                let maybeFeature =
                    featureRoutingMap
                    |> Map.tryFind featureArgs.Definition

                let t =
                    match maybeFeature with
                    | None -> unitTask { context.Response.StatusCode <- 404 }
                    | Some f ->
                        context.Items.Add("feature", { Feature = f; Args = featureArgs; AwaitState = false })
                        next.Invoke(context)

                return! t
            }

    type FeatureHandlingMiddleware(next: RequestDelegate, clusterClient: IClusterClient) =


        member this.InvokeAsync(context: HttpContext) =
            unitTask {
                let contextMap =
                    context.Items
                    |> Seq.map (fun kv -> (kv.Key |> string, kv.Value))
                    |> Map.ofSeq

                let maybeFeature =
                    match contextMap |> Map.tryFind "feature" with
                    | Some f ->
                        match f with
                        | :? FeatureCall as ftr -> Some ftr
                        | _ -> None
                    | _ -> None

                let t =
                    match maybeFeature with
                    | None -> unitTask { context.Response.StatusCode <- 404 }
                    | Some f ->
                        match f.Args.Value with
                        | AggregateRoot id ->
                            unitTask {
                                let grain =
                                    clusterClient.GetGrain<IAggregateGrain>($"agg_{id}")

                                let! result = grain.Call f

                                return!
                                    match result with
                                    | Ok -> unitTask { context.Response.StatusCode <- 200 }
                                    | RequestError _ -> unitTask { context.Response.StatusCode <- 400 }
                                    | ServerError _ -> unitTask { context.Response.StatusCode <- 500 }
                            }
                        | _ -> unitTask { context.Response.StatusCode <- 404 }

                return! t
            }
