namespace Metaflow

open System.Threading.Tasks
open Orleans

type IFeatureGrain<'op, 'model, 'input> =
    inherit IGrainWithStringKey
    abstract Call: FeatureCall<'input> -> Task<Result<'model option, FeatureFailure>>


type IConcurrencyScopeGrain =
    inherit IGrainWithStringKey
    abstract Execute<'op, 'model, 'input> : FeatureCall<'input> -> Task<Result<'model option, FeatureFailure>>
