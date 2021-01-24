namespace Metaflow


open System.Threading
open System.Threading.Tasks
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
      //      RequiredState: Type option
//      RequiredService: Type option
      Version: int }
    member this.Name =
        let titleCase =
            Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase

        let op =
            this.Operation.Name.ToLowerInvariant()
            |> titleCase

        $"{op}{this.Model.Name}V{this.Version}"



type UnitType = unit


type RequestContext = { RequestId: string }

[<Struct>]
type StepFailure =
    | InvalidOperation of Error: string
    | Exception of Exception: Exception

type StepResult =
    | Done
    | Skipped
    | Failed of StepFailure
    member this.Success =
        match this with
        | Done -> true
        | _ -> false

[<Serializable>]
[<Struct>]
type FeatureFailure =
    | NotFound
    | RequestError of Error: string
    | ServerError of Exception: Exception

[<Serializable>]
[<Struct>]
type WorkflowFailure =
    | StepFailure of StepFailureValue: StepFailure
    | FeatureFailure of FeatureFailureValue: FeatureFailure

[<Serializable>]
type FeatureOutput<'op, 'model> =
    | Done
    | Rejected
    | Exception of Exception

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
type FeatureInput<'model> =
    | Id of ModelId
    | IdAndModel of id: ModelId * obj: 'model

[<Serializable>]
type FeatureCall<'model> =
    { Feature: Feature
      AwaitState: bool
      Input: FeatureInput<'model> }
    member this.AggregateRootId =
        match this.Input with
        | Id mid -> mid.GetAggregateRootId()
        | IdAndModel (mid, _) -> mid.GetAggregateRootId()

    member this.ModelId =
        match this.Input with
        | Id mid -> mid.ToString()
        | IdAndModel (mid, _) -> mid.ToString()

    member this.Id =
        $"ftr:{this.Feature.Name}:{this.ModelId}"


type Step =
    { Name: string
      Handler: Type
      Workflow: string
      Background: bool }

type Workflow =
    { Name: string
      Feature: Feature
      Steps: Step list }

type State<'t> = { Value: 't option }

type Event = { Operation: Operation }

type IStepHandler<'model> =
    abstract Call: RequestContext * Result<unit, FeatureFailure> -> Task<StepResult>


type IRequires<'model> =
    abstract Check: FeatureInput<'model> -> Async<bool>
