namespace Metaflow


open System.Threading
open Microsoft.FSharp.Reflection
open System

[<Measure>]
type eventStream

[<Measure>]
type version

type Delete = Delete
type Post = Post
type Put = Put
type Patch = Patch

type Operation =
    | POST
    | PATCH
    | PUT
    | DELETE
    member this.Name =
        match FSharpValue.GetUnionFields(this, typeof<Operation>) with
        | case, _ -> case.Name

    member this.AsType() =
        match this with
        | DELETE -> typeof<Delete>
        | PATCH -> typeof<Patch>
        | POST -> typeof<Post>
        | PUT -> typeof<Put>

type ModelKind =
    | AggregateRoot of aggregate: string
    | OwnedValue of aggregate: string
    | OwnedEntity of aggregate: string

type ConcurrencyScope =
    | Aggregate
    | Entity
    | Feature

[<Serializable>]
type Feature =
    { Operation: Operation
      ConcurrencyScope: ConcurrencyScope
      Model: Type
      ModelKind: ModelKind
      RequiredState: Type option
      RequiredService: Type option
      Version: int }
    member this.Name =
        let titleCase =
            Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase

        let op =
            this.Operation.Name.ToLowerInvariant()
            |> titleCase

        $"{op}{this.Model.Name}V{this.Version}"


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

type UnitType = unit

[<Serializable>]
type FeatureResult =
    | Ok
    | NotFound
    | RequestError of string
    | ServerError of Exception



[<Serializable>]
type FeatureOutput<'op, 'model> =
    | Succeeded of 'model option
    | Rejected of string
    | Failed of Exception
    | Ignored of string

    member this.Name(feature: Feature) =
        let resource = typeof<'model>.Name

        let name (_: 'u) =
            match FSharpValue.GetUnionFields(this, typeof<'u>) with
            | case, _ -> case.Name

        let v = feature.Version
        let o = typeof<'op>.Name


        $"{(name this)}:{o}:{resource}:v{v}"



type AggregateRootId = { Id: string }

type OwnedValueId = { AggregateRootId: string }


type OwnedEntityId =
    { AggregateRootId: string
      EntityId: string }

type ModelId =
    | AggregateRootId of AggregateRootId
    | OwnedValueId of OwnedValueId
    | OwnedEntityId of OwnedEntityId
    override this.ToString() =
        match this with
        | AggregateRootId { Id = id } -> id
        | OwnedEntityId { AggregateRootId = rootId
                          EntityId = id } -> $"{rootId}:{id}"
        | OwnedValueId { AggregateRootId = rootId } -> $"{rootId}"

    member this.GetAggregateRootId() =
        match this with
        | AggregateRootId { Id = id } -> id
        | OwnedEntityId { AggregateRootId = rootId } -> rootId
        | OwnedValueId { AggregateRootId = rootId } -> rootId

[<Serializable>]
type FeatureInput<'input> =
    | Id of ModelId
    | IdAndObject of id: ModelId * obj: 'input

[<Serializable>]
type FeatureCall<'input> =
    { Feature: Feature
      AwaitState: bool
      Input: FeatureInput<'input> }
    member this.AggregateRootId =
        match this.Input with
        | Id mid -> mid.GetAggregateRootId()
        | IdAndObject (mid, _) -> mid.GetAggregateRootId()

    member this.ModelId =
        match this.Input with
        | Id mid -> mid.ToString()
        | IdAndObject (mid, _) -> mid.ToString()

    member this.Id =
        $"ftr:{this.Feature.Name}:{this.ModelId}"

type StateTrigger<'model> =
    { StateType: Type
      StateIdResolver: ModelId -> 'model -> string }

type FeatureHandler<'op,'model,'input> = {
    Feature: Feature
    Handler: FeatureInput<'input> -> Async<FeatureOutput<'op, 'model>>
}