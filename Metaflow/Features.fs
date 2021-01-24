namespace Metaflow


module Features =

    let workflow (feature: Feature) =
        { Name = feature.Name
          Feature = feature
          Steps = [] }

    let private step<'b, 'step when 'step :> IStepHandler<'b>> (workflow: Workflow) background =
        { workflow with

              Steps =
                  { Name = typeof<'step>.Name
                    Handler = typeof<'step>
                    Workflow = workflow.Name
                    Background = background }
                  :: workflow.Steps }

    let andf<'b, 'step when 'step :> IStepHandler<'b>> (workflow: Workflow) = step<'b, 'step> workflow false

    let andb<'b, 'step when 'step :> IStepHandler<'b>> (workflow: Workflow) = step<'b, 'step> workflow true

    let deleteValueFeature<'m> (aggregate: string) (ver: int) =

        { Operation = Operation.DELETE
          ConcurrencyScope = ConcurrencyScope.Aggregate
          Model = typeof<'m>
          ModelKind = ModelKind.OwnedValue aggregate
          Version = ver }

    let deleteValue<'m> (aggregate: string) (ver: int) =
        (aggregate, ver)
        ||> deleteValueFeature<'m>
        |> workflow
