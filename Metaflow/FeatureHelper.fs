namespace Metaflow

open Orleans
open FSharp.Control.Tasks
open Workflows

module FeatureHelper =


    let workflow (feature: Feature) =
        { Name = feature.Name
          Feature = feature
          Steps = [] }

    let private step<'a, 'b, 'c, 'step> (workflow: FeatureHandler<'a, 'b, 'c>) background =
        { workflow with
              Workflow =
                  { workflow.Workflow with
                        Steps =
                            { Name = typeof<'step>.Name
                              Handler = typeof<'step>
                              Workflow = workflow.Workflow.Name
                              Background = background }
                            :: workflow.Workflow.Steps } }

    let andf<'a, 'b, 'c, 'step> (workflow: FeatureHandler<'a, 'b, 'c>) = step<'a, 'b, 'c, 'step> workflow false
    let andb<'a, 'b, 'c, 'step> (workflow: FeatureHandler<'a, 'b, 'c>) = step<'a, 'b, 'c, 'step> workflow true

    let deleteValueFeature<'m> (aggregate: string) (ver: int) =

        { Operation = Operation.DELETE
          ConcurrencyScope = ConcurrencyScope.Aggregate
          Model = typeof<'m>
          ModelKind = ModelKind.OwnedValue aggregate
          RequiredService = None
          RequiredState = None
          Version = ver }

    let autoDone<'op, 'm> w: FeatureHandler<'op, 'm, unit> =
        let h =
            fun _ -> async { return FeatureOutput<'op, 'm>.Done None }

        { Workflow = w; Handler = h }

    let deleteValue<'m> (aggregate: string) (ver: int) =
        (aggregate, ver)
        ||> deleteValueFeature
        |> workflow
        |> autoDone<Delete, 'm>
