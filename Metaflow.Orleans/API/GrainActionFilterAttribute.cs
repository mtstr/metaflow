using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Metaflow.Orleans
{
    public class GrainActionFilterAttribute : Attribute, IAsyncActionFilter
    {
        private readonly IEnumerable<IGrainActionFilter> _filters;

        public GrainActionFilterAttribute(IEnumerable<IGrainActionFilter> filters)
        {
            _filters = filters ?? new List<IGrainActionFilter>();
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var resultContext = await next();

            foreach (var f in _filters)
            {
                if (context.ActionArguments.ContainsKey("id"))
                {
                    await f.Invoke(context.ActionArguments["id"].ToString(), resultContext);
                }
            }
        }
    }

}
