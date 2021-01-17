using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Metaflow
{
    public class WorkflowsClientBuilder
    {
        private readonly HashSet<Assembly> _assemblies = new();
        public IReadOnlyCollection<Assembly> Assemblies => _assemblies.ToList().AsReadOnly();

        public WorkflowsClientBuilder Add<T>()
        {
            _assemblies.Add(typeof(T).Assembly);

            return this;
        }
    }
}