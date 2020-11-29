namespace Metaflow

open Microsoft.FSharp.Reflection
open System
open System.Threading.Tasks

type Operation =
    | GET
    | POST
    | PATCH
    | PUT
    | DELETE
    member this.Name() =
        match FSharpValue.GetUnionFields(this, typeof<Operation>) with
        | case, _ -> case.Name

type EventPayload<'T> =
    { Version: int
      Before: 'T option
      After: 'T option
      RequestId: string
      Message: string option
      Feature: string
      Operation: Operation }

type Event<'T> =
    | Updated of EventPayload<'T>
    | Replaced of EventPayload<'T>
    | Deleted of EventPayload<'T>
    | Created of EventPayload<'T>
    | Upgraded of EventPayload<'T>
    | Ignored of EventPayload<'T>
    | Rejected of EventPayload<'T>
    | Failed of EventPayload<'T>

    member this.Name() =
        let resource = typeof<'T>

        let name (u: 'u) =
            match FSharpValue.GetUnionFields(this, typeof<'u>) with
            | case, _ -> case.Name

        let suffix =
            match this with
            | Created { Version = v } -> v |> string
            | Deleted { Version = v } -> v |> string
            | Upgraded { Version = v } -> v |> string
            | Updated { Version = v } -> v |> string
            | Replaced { Version = v } -> v |> string
            | Ignored { Version = v; Operation = o } -> $"{v}:{name o}"
            | Rejected { Version = v; Operation = o } -> $"{v}:{name o}"
            | Failed { Version = v; Operation = o } -> $"{v}:{name o}"

        $"{(name this)}:{resource}:{suffix}"

[<AttributeUsage(AttributeTargets.Method)>]
type FeatureAttribute(Operation: Operation, argType: Type, version: int) =
    inherit Attribute()

    let scope: Type option = None

    new(Operation: Operation, argType: Type) = FeatureAttribute(Operation, argType, 1)

    member __.Version: int = version
    member __.Operation: Operation = Operation
    member __.ArgType: Type = argType
    member val Scope = scope with get, set

type Feature =
    { Operation: Operation
      Model: Type
      Scope: Type option
      Name: string
      Version: int }

type Result =
    | Success
    | Error of string
    | Ignore of string

type IDispatcher<'TState> =
    abstract Invoke<'TResource, 'TInput> : Operation * 'TState * 'TInput -> Task<Event<'TResource>>
