using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Diagnostics.StatusCodePagesOverrides
{
    public static class StatusCodePagesOverrideExtensions
    {
        /// <summary>
        /// Adds a StatusCodePages middleware with the given options that checks for responses with status codes
        /// between 400 and 599 that do not have a body.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseStatusCodePages(this IApplicationBuilder app, StatusCodePagesOptions options, int statusCode)
        {
            return app.UseMiddleware<StatusCodePagesOverrideMiddleware>(options, statusCode);
        }

        /// <summary>
        /// Adds a StatusCodePages middleware with a default response handler that checks for responses with status codes
        /// between 400 and 599 that do not have a body.
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseStatusCodePages(this IApplicationBuilder app, int statusCode)
        {
            return UseStatusCodePages(app, new StatusCodePagesOptions(), statusCode);
        }

        /// <summary>
        /// Adds a StatusCodePages middleware with the specified handler that checks for responses with status codes
        /// between 400 and 599 that do not have a body.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseStatusCodePages(this IApplicationBuilder app, Func<StatusCodeContext, Task> handler, int statusCode)
        {
            return UseStatusCodePages(app, new StatusCodePagesOptions() { HandleAsync = handler }, statusCode);
        }

        /// <summary>
        /// Adds a StatusCodePages middleware with the specified response body to send. This may include a '{0}' placeholder for the status code.
        /// The middleware checks for responses with status codes between 400 and 599 that do not have a body.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="contentType"></param>
        /// <param name="bodyFormat"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseStatusCodePages(this IApplicationBuilder app, string contentType, string bodyFormat, int statusCode)
        {
            return UseStatusCodePages(app, context =>
            {
                var body = string.Format(CultureInfo.InvariantCulture, bodyFormat, context.HttpContext.Response.StatusCode);
                context.HttpContext.Response.ContentType = contentType;
                return context.HttpContext.Response.WriteAsync(body);
            }, statusCode);
        }

        /// <summary>
        /// Adds a StatusCodePages middleware to the pipeine. Specifies that responses should be handled by redirecting
        /// with the given location URL template. This may include a '{0}' placeholder for the status code. URLs starting
        /// with '~' will have PathBase prepended, where any other URL will be used as is.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="locationFormat"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseStatusCodePagesWithRedirects(this IApplicationBuilder app, string locationFormat, int statusCode)
        {
            if (locationFormat.StartsWith("~"))
            {
                locationFormat = locationFormat.Substring(1);
                return UseStatusCodePages(app, context =>
                {
                    var location = string.Format(CultureInfo.InvariantCulture, locationFormat, context.HttpContext.Response.StatusCode);
                    context.HttpContext.Response.Redirect(context.HttpContext.Request.PathBase + location);
                    return Task.FromResult(0);
                }, statusCode);
            }
            else
            {
                return UseStatusCodePages(app, context =>
                {
                    var location = string.Format(CultureInfo.InvariantCulture, locationFormat, context.HttpContext.Response.StatusCode);
                    context.HttpContext.Response.Redirect(location);
                    return Task.FromResult(0);
                }, statusCode);
            }
        }

        /// <summary>
        /// Adds a StatusCodePages middleware to the pipeline with the specified alternate middleware pipeline to execute
        /// to generate the response body.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseStatusCodePages(this IApplicationBuilder app, Action<IApplicationBuilder> configuration, int statusCode)
        {
            var builder = app.New();
            configuration(builder);
            var tangent = builder.Build();
            return UseStatusCodePages(app, context => tangent(context.HttpContext), statusCode);
        }

        /// <summary>
        /// Adds a StatusCodePages middleware to the pipeline. Specifies that the response body should be generated by
        /// re-executing the request pipeline using an alternate path. This path may contain a '{0}' placeholder of the status code.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="pathFormat"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseStatusCodePagesWithReExecute(this IApplicationBuilder app, string pathFormat, int statusCode)
        {
            return UseStatusCodePages(app, async context =>
            {
                var newPath = new PathString(string.Format(CultureInfo.InvariantCulture, pathFormat, context.HttpContext.Response.StatusCode));

                var originalPath = context.HttpContext.Request.Path;
                // Store the original paths so the app can check it.
                context.HttpContext.Features.Set<IStatusCodeReExecuteFeature>(new StatusCodeReExecuteFeature()
                {
                    OriginalPathBase = context.HttpContext.Request.PathBase.Value,
                    OriginalPath = originalPath.Value,
                });

                context.HttpContext.Request.Path = newPath;
                try
                {
                    await context.Next(context.HttpContext);
                }
                finally
                {
                    context.HttpContext.Request.Path = originalPath;
                    context.HttpContext.Features.Set<IStatusCodeReExecuteFeature>(null);
                }
            }, statusCode);
        }
    }
}