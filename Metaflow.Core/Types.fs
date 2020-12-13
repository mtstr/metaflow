namespace Metaflow

open Microsoft.FSharp.Reflection
open System
open System.Threading.Tasks

type EventStream = { Name: string; Version: int }

type Operation =
    | POST
    | PATCH
    | PUT
    | DELETE
    member this.Name() =
        match FSharpValue.GetUnionFields(this, typeof<Operation>) with
        | case, _ -> case.Name

type Feature =
    { Operation: Operation
      Scoped: bool
      Model: Type
      RequiredState: Type option
      RequiredService: Type option
      Name: string
      Version: int }

type Success<'T> =
    { Version: int
      Before: 'T option
      After: 'T option
      RequestId: string
      Feature: string
      Operation: Operation }

type Nonsuccess<'T> =
    { Version: int
      RequestId: string
      Message: string
      Feature: string
      Operation: Operation }

[<AttributeUsage(AttributeTargets.Method)>]
type FeatureAttribute(Operation: Operation, argType: Type, version: int) =
    inherit Attribute()
    let scoped = false
    new(op: Operation, argType: Type) = FeatureAttribute(op, argType, 1)

    member __.Version: int = version
    member __.Operation: Operation = Operation
    member __.ArgType: Type = argType
    member val Scoped = scoped with get, set



type UnitType = unit

type ModelChange<'model> =
    { Before: 'model option
      After: 'model option }

type FeatureResult =
    | Ok
    | RequestError of string
    | ServerError of string

type FeatureOutput<'model> =
    | Success of ModelChange<'model>
    | Reject of string
    | Failure of string
    | Ignore of string


type Event<'model> =
    | Updated of Success<'model>
    | Replaced of Success<'model>
    | Deleted of Success<'model>
    | Created of Success<'model>
    | Ignored of Nonsuccess<'model>
    | Rejected of Nonsuccess<'model>
    | Failed of Nonsuccess<'model>

    static member FromOutput(output: FeatureOutput<'model>, feature: Feature) =

        match output with
        | Success so ->
            Created
                { Version = 0
                  Before = so.Before
                  After = so.After
                  RequestId = ""
                  Feature = feature.Name
                  Operation = feature.Operation }
        | Ignore ig ->
            Ignored
                { Version = 0
                  Message = ig
                  RequestId = ""
                  Feature = feature.Name
                  Operation = feature.Operation }
        | Failure err ->
            Failed
                { Version = 0
                  Message = err
                  RequestId = ""
                  Feature = feature.Name
                  Operation = feature.Operation }
        | Reject err ->
            Rejected
                { Version = 0
                  Message = err
                  RequestId = ""
                  Feature = feature.Name
                  Operation = feature.Operation }

    member this.Name() =
        let resource = typeof<'model>

        let name (u: 'u) =
            match FSharpValue.GetUnionFields(this, typeof<'u>) with
            | case, _ -> case.Name

        let suffix =
            match this with
            | Created { Version = v } -> v |> string
            | Deleted { Version = v } -> v |> string
            | Updated { Version = v } -> v |> string
            | Replaced { Version = v } -> v |> string
            | Ignored { Version = v; Operation = o } -> $"{v}:{name o}"
            | Rejected { Version = v; Operation = o } -> $"{v}:{name o}"
            | Failed { Version = v; Operation = o } -> $"{v}:{name o}"

        $"{(name this)}:{resource}:{suffix}"

type FeatureArgsDefinition =
    | Unknown
    | AggregateRoot of string
    | OwnedValue of string
    | OwnedEntity of string

type AggregateRootId = { Id: string }

type OwnedValueId = { AggregateRootId: string }

type OwnedEntityId =
    { AggregateRootId: string
      EntityId: string }

type FeatureArgValue =
    | AggregateRoot of AggregateRootId
    | OwnedValue of OwnedValueId
    | OwnedEntity of OwnedEntityId

type FeatureCallArgs =
    { Definition: FeatureArgsDefinition
      Value: FeatureArgValue }

type FeatureCall =
    { Feature: Feature
      AwaitState: bool
      Args: FeatureCallArgs }

type FeatureHandler<'model, 'state, 'di> =
    | AggregateRootWithState of (AggregateRootId -> 'state -> FeatureOutput<'model>)
    | AggregateRootWithStateAndDI of (AggregateRootId -> 'state -> 'di -> FeatureOutput<'model>)
    | AggregateRootWithDI of (AggregateRootId -> 'di -> FeatureOutput<'model>)

type StateHandler<'model, 'state> = Event<'model> -> Async<'state>

type StateObserver<'model> =
    { StateType: Type
      StateIdResolver: 'model -> string }


type IFeatureService<'service> =
    abstract Get: unit -> 'service option

type HandlerDep<'service>(service: 'service option) =
    interface IFeatureService<'service> with
        member this.Get() =
            match typeof<'service> with
            | u when typeof<unit> = u -> None
            | _ -> service

type IDispatcher<'TState> =
    abstract Invoke<'TResource, 'TInput> : Operation * 'TState * 'TInput -> Task<Event<'TResource>>
