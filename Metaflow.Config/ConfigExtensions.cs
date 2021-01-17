using Microsoft.FSharp.Core;

namespace Metaflow
{
    public static class ConfigExtensions
    {
        public static FeatureHandler<Delete, TModel, Unit> Then<TModel, TH>(this FeatureHandler<Delete, TModel, Unit> h)
            where TH : IStepHandler<TModel>
        {
            return Features.andf<Delete, TModel, Unit, TH>(h);
        }

        public static FeatureHandler<T1, T2, T3> Then<T1, T2, T3, TH>(this FeatureHandler<T1, T2, T3> h)
            where TH : IStepHandler<T2>
        {
            return Features.andf<T1, T2, T3, TH>(h);
        }

        public static FeatureHandler<T1, T2, T3> ThenInBackground<T1, T2, T3, TH>(this FeatureHandler<T1, T2, T3> h)
            where TH : IStepHandler<T2>
        {
            return Features.andb<T1, T2, T3, TH>(h);
        }
    }
}