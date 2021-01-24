using Microsoft.FSharp.Core;

namespace Metaflow
{
    public static class ConfigExtensions
    {
        public static Workflow Then<TModel, TH>(this Workflow h)
            where TH : IStepHandler<TModel>
        {
            return Features.andf<TModel, TH>(h);
        }


        public static Workflow ThenInBackground<T1, T2, T3, TH>(this Workflow h)
            where TH : IStepHandler<T2>
        {
            return Features.andb<T2, TH>(h);
        }
    }
}