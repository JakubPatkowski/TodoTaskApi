using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TodoTaskAPI.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Custom startup filter for test environment
    /// Adds logging and monitoring of test requests
    /// </summary>
    public class TestStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.Use(async (context, nextMiddleware) =>
                {
                    var logger = context.RequestServices
                        .GetRequiredService<ILogger<TestStartupFilter>>();

                    logger.LogInformation(
                        "Processing request: {Method} {Path}",
                        context.Request.Method,
                        context.Request.Path);

                    await nextMiddleware();

                    logger.LogInformation(
                        "Completed request: {Method} {Path} - Status: {StatusCode}",
                        context.Request.Method,
                        context.Request.Path,
                        context.Response.StatusCode);
                });

                next(app);
            };
        }
    }
}
