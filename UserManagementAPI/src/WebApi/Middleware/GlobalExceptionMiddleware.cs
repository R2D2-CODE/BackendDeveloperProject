using Application.DTOs;
using System.Net;
using System.Text.Json;

namespace WebApi.Middleware;

/// <summary>
/// Global exception handling middleware
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = exception switch
        {
            ArgumentNullException => new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid input: null value provided",
                Errors = new List<string> { exception.Message }
            },
            ArgumentException => new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid input provided",
                Errors = new List<string> { exception.Message }
            },
            UnauthorizedAccessException => new ApiResponse<object>
            {
                Success = false,
                Message = "Unauthorized access",
                Errors = new List<string> { "You don't have permission to access this resource" }
            },
            KeyNotFoundException => new ApiResponse<object>
            {
                Success = false,
                Message = "Resource not found",
                Errors = new List<string> { exception.Message }
            },
            TimeoutException => new ApiResponse<object>
            {
                Success = false,
                Message = "Request timeout",
                Errors = new List<string> { "The operation took too long to complete" }
            },
            _ => new ApiResponse<object>
            {
                Success = false,
                Message = "An internal server error occurred",
                Errors = new List<string> { "Please try again later or contact support" }
            }
        };

        response.StatusCode = exception switch
        {
            ArgumentNullException or ArgumentException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            TimeoutException => (int)HttpStatusCode.RequestTimeout,
            _ => (int)HttpStatusCode.InternalServerError
        };

        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(jsonResponse);
    }
}
