using Application.DTOs;
using System.Net;
using System.Text.Json;

namespace WebApi.Middleware;

/// <summary>
/// Global exception handling middleware for TechHive Solutions compliance
/// Catches unhandled exceptions and returns standardized error responses
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Response.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
        
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Enhanced logging for TechHive auditing requirements
            _logger.LogError(ex, 
                "EXCEPTION - CorrelationId: {CorrelationId} | Path: {Path} | Method: {Method} | " +
                "UserId: {UserId} | ClientIP: {ClientIP} | Exception: {ExceptionType} | Message: {Message}",
                correlationId,
                context.Request.Path,
                context.Request.Method,
                context.User?.Identity?.Name ?? "Anonymous",
                GetClientIpAddress(context),
                ex.GetType().Name,
                ex.Message);
                
            await HandleExceptionAsync(context, ex, correlationId);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        // Standardized error response format for TechHive Solutions
        var errorResponse = exception switch
        {
            ArgumentNullException argNullEx => new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid input: required value is missing",
                Errors = new List<string> { _environment.IsDevelopment() ? argNullEx.Message : "Required parameter is null" },
                Data = new { CorrelationId = correlationId, Timestamp = DateTime.UtcNow }
            },
            
            ArgumentException argEx => new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid input provided",
                Errors = new List<string> { _environment.IsDevelopment() ? argEx.Message : "Invalid parameter value" },
                Data = new { CorrelationId = correlationId, Timestamp = DateTime.UtcNow }
            },
            
            UnauthorizedAccessException => new ApiResponse<object>
            {
                Success = false,
                Message = "Access denied",
                Errors = new List<string> { "You are not authorized to access this resource" },
                Data = new { CorrelationId = correlationId, Timestamp = DateTime.UtcNow }
            },
            
            KeyNotFoundException notFoundEx => new ApiResponse<object>
            {
                Success = false,
                Message = "Resource not found",
                Errors = new List<string> { _environment.IsDevelopment() ? notFoundEx.Message : "The requested resource was not found" },
                Data = new { CorrelationId = correlationId, Timestamp = DateTime.UtcNow }
            },
            
            InvalidOperationException invalidOpEx => new ApiResponse<object>
            {
                Success = false,
                Message = "Operation failed",
                Errors = new List<string> { _environment.IsDevelopment() ? invalidOpEx.Message : "The requested operation could not be completed" },
                Data = new { CorrelationId = correlationId, Timestamp = DateTime.UtcNow }
            },
            
            TimeoutException => new ApiResponse<object>
            {
                Success = false,
                Message = "Request timeout",
                Errors = new List<string> { "The request took too long to process" },
                Data = new { CorrelationId = correlationId, Timestamp = DateTime.UtcNow }
            },
            
            _ => new ApiResponse<object>
            {
                Success = false,
                Message = "Internal server error",
                Errors = new List<string> { _environment.IsDevelopment() ? exception.Message : "An unexpected error occurred" },
                Data = new { CorrelationId = correlationId, Timestamp = DateTime.UtcNow }
            }
        };

        // Set appropriate HTTP status codes
        response.StatusCode = exception switch
        {
            ArgumentNullException or ArgumentException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            InvalidOperationException => (int)HttpStatusCode.Conflict,
            TimeoutException => (int)HttpStatusCode.RequestTimeout,
            _ => (int)HttpStatusCode.InternalServerError
        };

        // Serialize and write response
        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        });

        await response.WriteAsync(jsonResponse);
    }

    private string GetClientIpAddress(HttpContext context)
    {
        var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            return xForwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
