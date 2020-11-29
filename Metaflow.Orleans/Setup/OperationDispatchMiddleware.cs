using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Metaflow.Orleans
{
    public class OperationDispatchMiddleware
    {
        private readonly RequestDelegate _next;

        public OperationDispatchMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var cultureQuery = context.Request.Query["culture"];
            

            // Call the next delegate/middleware in the pipeline
            await _next(context);
        }
    }
}
