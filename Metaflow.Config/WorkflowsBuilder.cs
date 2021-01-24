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
        private readonly HashSet<Assembly> _assemblies = new();
        public IServiceCollection Services { get; set; }


        public IReadOnlyCollection<Assembly> Assemblies => _assemblies.ToList().AsReadOnly();

        public WorkflowsBuilder Delete<TModel>(string aggregate, int version = 1,
            Func<Workflow, Workflow> f = null)
        {
            var w = Features.deleteValue<TModel>(aggregate, version);
            if (f != null) w = f(w);
            return AddWorkflow<TModel>(w);
        }

        private WorkflowsBuilder AddWorkflow<TModel>(Workflow h)
        {
            if (Services != null)
            {
                Services.AddSingleton(_ => h);
                foreach (var step in h.Steps) Services.AddScoped(step.Handler);
            }

            _assemblies.Add(typeof(TModel).Assembly);

            return this;
        }
    }
}