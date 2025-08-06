using System.Diagnostics;
using System.Text;

namespace WebApi.Middleware;

/// <summary>
/// Middleware for comprehensive request/response logging to comply with TechHive Solutions auditing requirements
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Guid.NewGuid().ToString();
        
        // Add correlation ID to response headers for tracking
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        
        // Log incoming request
        await LogRequestAsync(context, correlationId);

        // Capture original response body stream
        var originalResponseBodyStream = context.Response.Body;
        
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            // Continue to next middleware
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            // Log outgoing response
            await LogResponseAsync(context, correlationId, stopwatch.ElapsedMilliseconds);
            
            // Copy the response body back to the original stream
            await responseBody.CopyToAsync(originalResponseBodyStream);
        }
    }

    private async Task LogRequestAsync(HttpContext context, string correlationId)
    {
        var request = context.Request;
        
        // Capture request body for POST/PUT requests
        string requestBody = "";
        if (request.ContentLength > 0 && (request.Method == "POST" || request.Method == "PUT" || request.Method == "PATCH"))
        {
            request.EnableBuffering();
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            request.Body.Position = 0;
        }

        // Get user information if available
        var userId = context.User?.Identity?.Name ?? "Anonymous";
        var userAgent = request.Headers["User-Agent"].ToString();
        var clientIp = GetClientIpAddress(context);

        _logger.LogInformation(
            "REQUEST - CorrelationId: {CorrelationId} | Method: {Method} | Path: {Path} | QueryString: {QueryString} | " +
            "UserId: {UserId} | ClientIP: {ClientIP} | UserAgent: {UserAgent} | ContentType: {ContentType} | " +
            "ContentLength: {ContentLength} | RequestBody: {RequestBody}",
            correlationId,
            request.Method,
            request.Path,
            request.QueryString.ToString(),
            userId,
            clientIp,
            userAgent,
            request.ContentType ?? "N/A",
            request.ContentLength ?? 0,
            SanitizeRequestBody(requestBody, request.ContentType)
        );
    }

    private async Task LogResponseAsync(HttpContext context, string correlationId, long elapsedMilliseconds)
    {
        var response = context.Response;
        
        // Capture response body for logging (limit size for performance)
        string responseBody = "";
        if (response.Body.CanSeek && response.Body.Length > 0)
        {
            response.Body.Position = 0;
            using var reader = new StreamReader(response.Body, Encoding.UTF8, leaveOpen: true);
            responseBody = await reader.ReadToEndAsync();
            response.Body.Position = 0;
            
            // Limit response body logging to prevent excessive log sizes
            if (responseBody.Length > 1000)
            {
                responseBody = responseBody.Substring(0, 1000) + "... [truncated]";
            }
        }

        var logLevel = GetLogLevelByStatusCode(response.StatusCode);
        
        _logger.Log(logLevel,
            "RESPONSE - CorrelationId: {CorrelationId} | StatusCode: {StatusCode} | ContentType: {ContentType} | " +
            "ContentLength: {ContentLength} | Duration: {Duration}ms | ResponseBody: {ResponseBody}",
            correlationId,
            response.StatusCode,
            response.ContentType ?? "N/A",
            response.ContentLength ?? response.Body.Length,
            elapsedMilliseconds,
            SanitizeResponseBody(responseBody, response.ContentType)
        );
    }

    private string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP addresses (in case of proxy/load balancer)
        var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            return xForwardedFor.Split(',')[0].Trim();
        }

        var xRealIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xRealIp))
        {
            return xRealIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private string SanitizeRequestBody(string requestBody, string? contentType)
    {
        if (string.IsNullOrEmpty(requestBody))
            return "";

        // Don't log sensitive data or large payloads
        if (contentType?.Contains("application/json") == true)
        {
            // Remove or mask sensitive fields like passwords, tokens, etc.
            if (requestBody.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                requestBody.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                requestBody.Contains("secret", StringComparison.OrdinalIgnoreCase))
            {
                return "[SENSITIVE DATA MASKED]";
            }
            
            // Limit size
            return requestBody.Length > 500 ? requestBody.Substring(0, 500) + "... [truncated]" : requestBody;
        }

        return "[NON-JSON CONTENT]";
    }

    private string SanitizeResponseBody(string responseBody, string? contentType)
    {
        if (string.IsNullOrEmpty(responseBody))
            return "";

        // Don't log sensitive response data
        if (responseBody.Contains("token", StringComparison.OrdinalIgnoreCase) ||
            responseBody.Contains("password", StringComparison.OrdinalIgnoreCase))
        {
            return "[SENSITIVE DATA MASKED]";
        }

        return responseBody;
    }

    private LogLevel GetLogLevelByStatusCode(int statusCode)
    {
        return statusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            >= 300 => LogLevel.Information,
            _ => LogLevel.Information
        };
    }
}
