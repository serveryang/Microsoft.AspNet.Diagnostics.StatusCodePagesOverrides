using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Diagnostics.StatusCodePagesOverrides
{
    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
    public class StatusCodePagesOverrideMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly StatusCodePagesOptions _options;
        private readonly int _statusCode;

        public StatusCodePagesOverrideMiddleware(RequestDelegate next, StatusCodePagesOptions options, int statusCode)
        {
            _next = next;
            _options = options;
            if (_options.HandleAsync == null)
            {
                throw new ArgumentException("Missing options.HandleAsync implementation.");
            }
            _statusCode = statusCode;
        }

        public async Task Invoke(HttpContext context)
        {
            var statusCodeFeature = new StatusCodePagesFeature();
            context.Features.Set<IStatusCodePagesFeature>(statusCodeFeature);

            await _next(context);

            if (!statusCodeFeature.Enabled)
            {
                // Check if the feature is still available because other middleware (such as a web API written in MVC) could
                // have disabled the feature to prevent HTML status code responses from showing up to an API client.
                return;
            }

            // Do nothing if a response body has already been provided.
            if (context.Response.HasStarted
                || context.Response.StatusCode < 400
                || context.Response.StatusCode >= 600
                || context.Response.StatusCode != _statusCode // additional check
                || context.Response.ContentLength.HasValue
                || !string.IsNullOrEmpty(context.Response.ContentType))
            {
                return;
            }

            var statusCodeContext = new StatusCodeContext(context, _options, _next);
            await _options.HandleAsync(statusCodeContext);
        }
    }
}