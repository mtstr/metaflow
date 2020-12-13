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
    abstract Call<'model> : ModelChange<'model> -> Task


type StateGrain<'state>([<PersistentState("domainState")>] state: IPersistentState<'state>,
                        stateHandlers: IDictionary<Type, obj -> Async<unit>>,
                        logger: ILogger<StateGrain<'state>>) =
    inherit Grain()


    interface IStateGrain<'state> with
        member this.Call<'model>(modelChange: ModelChange<'model>) =
            unitTask {
                match stateHandlers.ContainsKey(typeof<'model>) with
                | true ->
                    do! stateHandlers.Item typeof<'model> (modelChange :> obj)
                    do! state.WriteStateAsync()
                | _ -> ()
            }
