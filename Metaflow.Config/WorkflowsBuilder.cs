using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FSharp.Core;

namespace Metaflow
{
    public class WorkflowsBuilder
    {
        public IServiceCollection Services { get; set; }


        public IReadOnlyCollection<Assembly> Assemblies => _assemblies.ToList().AsReadOnly();
        private readonly HashSet<Assembly> _assemblies = new();

        private WorkflowsBuilder AddWorkflow<TOp, TModel, TInput>(FeatureHandler<TOp, TModel, TInput> h)
        {
            if (Services != null)
            {
                Services.AddSingleton(_ => h.Workflow);
                Services.AddSingleton(_ => h);
                foreach (var step in h.Workflow.Steps)
                {
                    Services.AddScoped(step.Handler);
                }
            }

            _assemblies.Add(typeof(TModel).Assembly);
            _assemblies.Add(typeof(TInput).Assembly);

            

            return this;
        }

        public WorkflowsBuilder Delete<TModel>(string aggregate, int version = 1,
            Func<FeatureHandler<Delete, TModel, Unit>, FeatureHandler<Delete, TModel, Unit>> f = null)
        {
            FeatureHandler<Delete, TModel, Unit> w = FeatureHelper.deleteValue<TModel>(aggregate, version);
            if (f != null) w = f(w);
            return AddWorkflow(w);
        }
    }
}