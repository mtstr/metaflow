namespace Metaflow

open System
open Metaflow
open Orleans
open System.Threading.Tasks
open FSharp.Control.Tasks
open Microsoft.Extensions.Logging
open Orleans.Runtime
open System.Collections.Generic

type IStateGrain<'state> =
    inherit IGrainWithStringKey
    abstract Call<'op,'model> : FeatureOutput<'op,'model> -> Task


type StateGrain<'state>([<PersistentState("domainState")>] state: IPersistentState<'state>,
                        stateHandlers: IDictionary<Type, 'state -> obj -> Async<'state>>,
                        logger: ILogger<StateGrain<'state>>) =
    inherit Grain()


    interface IStateGrain<'state> with
        member this.Call<'op,'model>(modelChange: FeatureOutput<'op,'model>) =
            unitTask {
                match stateHandlers.ContainsKey(typeof<'model>) with
                | true ->
                    let! newState = stateHandlers.Item typeof<'model> state.State (modelChange :> obj)
                    state.State <- newState
                    do! state.WriteStateAsync()
                | _ -> ()
            }
