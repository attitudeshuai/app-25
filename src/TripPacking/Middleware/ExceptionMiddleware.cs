using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using TripPacking.DTOs;

namespace TripPacking.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized,
                ApiResponse<object>.Fail("Unauthorized", StatusCodes.Status401Unauthorized)),
            ArgumentException => (StatusCodes.Status400BadRequest,
                ApiResponse<object>.Fail(exception.Message, StatusCodes.Status400BadRequest)),
            KeyNotFoundException => (StatusCodes.Status404NotFound,
                ApiResponse<object>.Fail(exception.Message, StatusCodes.Status404NotFound)),
            _ => (StatusCodes.Status500InternalServerError,
                ApiResponse<object>.Fail("Internal server error", StatusCodes.Status500InternalServerError))
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionMiddleware>();
    }
}
