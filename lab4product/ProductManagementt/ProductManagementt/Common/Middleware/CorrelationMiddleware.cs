using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ProductManagementt.Common.Middleware;

public class CorrelationMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        //generate a new correlation id
        var correlationId = Guid.NewGuid().ToString();
        context.Items["CorrelationId"] = correlationId;

        // Optionally set in response headers too for client tracing
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // Add to logger scope
        var logger = context.RequestServices.GetRequiredService<ILogger<CorrelationMiddleware>>();
        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context);
        }
    }
}