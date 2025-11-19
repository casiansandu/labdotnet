
using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProductManagementt.Exceptions;

namespace ProductManagementt.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<GlobalExceptionMiddleware> logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred. TraceId: {TraceId}", context.TraceIdentifier);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        ErrorResponse errorResponse;
        int statusCode;

        // Handle FluentValidation exceptions (thrown by validators)
        if (exception is FluentValidation.ValidationException fvEx)
        {
            var details = fvEx.Errors.Select(e => e.ErrorMessage).ToList();
            errorResponse = new ErrorResponse("VALIDATION_ERROR", "Validation failed", details)
            {
                TraceId = context.TraceIdentifier
            };
            statusCode = StatusCodes.Status400BadRequest;
        }
        else if (exception is ProductManagementt.Exceptions.ValidationException customValEx)
        {
            errorResponse = new ErrorResponse(customValEx.ErrorCode, customValEx.Message, customValEx.Errors)
            {
                TraceId = context.TraceIdentifier
            };
            statusCode = customValEx.StatusCode;
        }
        else if (exception is DbUpdateException dbEx)
        {
            // Database update errors - return 409 Conflict with limited details
            var details = new List<string> { dbEx.InnerException?.Message ?? dbEx.Message };
            errorResponse = new ErrorResponse("DB_UPDATE_ERROR", "A database update error occurred", details)
            {
                TraceId = context.TraceIdentifier
            };
            statusCode = StatusCodes.Status409Conflict;
        }
        else if (exception is BaseException baseEx)
        {
            errorResponse = new ErrorResponse(baseEx.ErrorCode, baseEx.Message)
            {
                TraceId = context.TraceIdentifier
            };
            statusCode = baseEx.StatusCode;
        }
        else
        {
            errorResponse = new ErrorResponse("INTERNAL_SERVER_ERROR", "An unexpected error occurred")
            {
                TraceId = context.TraceIdentifier
            };
            statusCode = (int)HttpStatusCode.InternalServerError;
        }

        context.Response.StatusCode = statusCode;
        var response = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(response);
    }
}