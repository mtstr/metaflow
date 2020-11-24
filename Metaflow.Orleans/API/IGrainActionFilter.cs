using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Metaflow.Orleans
{
    public interface IGrainActionFilter
    {
        Task Invoke(string grainId, ActionExecutedContext resultContext);
    }

}
