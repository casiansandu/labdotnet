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
        var correlationId = Guid.NewGuid().ToString();
        context.Items["CorrelationId"] = correlationId;

        context.Response.Headers[CorrelationIdHeader] = correlationId;

        var logger = context.RequestServices.GetRequiredService<ILogger<CorrelationMiddleware>>();
        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context);
        }
    }
}