using Application.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace WebApi.Middleware;

/// <summary>
/// Token-based authentication middleware for TechHive Solutions API security
/// Validates JWT tokens and enforces authentication on protected endpoints
/// </summary>
public class TokenAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TokenAuthenticationMiddleware> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _secretKey;
    private readonly TokenValidationParameters _tokenValidationParameters;

    // Endpoints that don't require authentication
    private readonly HashSet<string> _publicEndpoints = new(StringComparer.OrdinalIgnoreCase)
    {
        "/",
        "/health",
        "/info",
        "/swagger",
        "/swagger/index.html",
        "/swagger/v1/swagger.json",
        "/api/auth/login",
        "/api/auth/register"
    };

    public TokenAuthenticationMiddleware(RequestDelegate next, ILogger<TokenAuthenticationMiddleware> logger, IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
        
        // Use a default secret key for demonstration (in production, use a secure key from configuration)
        _secretKey = _configuration["JwtSettings:SecretKey"] ?? "TechHive-Super-Secret-Key-For-JWT-Tokens-2024!";
        
        _tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey)),
            ValidateIssuer = true,
            ValidIssuer = _configuration["JwtSettings:Issuer"] ?? "TechHive.UserManagementAPI",
            ValidateAudience = true,
            ValidAudience = _configuration["JwtSettings:Audience"] ?? "TechHive.Users",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Response.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
        var path = context.Request.Path.Value ?? "";
        
        // Skip authentication for public endpoints and Swagger
        if (IsPublicEndpoint(path))
        {
            _logger.LogInformation(
                "AUTH SKIP - CorrelationId: {CorrelationId} | Path: {Path} | Reason: Public endpoint",
                correlationId, path);
            await _next(context);
            return;
        }

        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        
        if (string.IsNullOrEmpty(authHeader))
        {
            _logger.LogWarning(
                "AUTH FAILED - CorrelationId: {CorrelationId} | Path: {Path} | ClientIP: {ClientIP} | Reason: Missing Authorization header",
                correlationId, path, GetClientIpAddress(context));
                
            await WriteUnauthorizedResponse(context, "Missing Authorization header", correlationId);
            return;
        }

        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "AUTH FAILED - CorrelationId: {CorrelationId} | Path: {Path} | ClientIP: {ClientIP} | Reason: Invalid Authorization header format",
                correlationId, path, GetClientIpAddress(context));
                
            await WriteUnauthorizedResponse(context, "Invalid Authorization header format. Use 'Bearer <token>'", correlationId);
            return;
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning(
                "AUTH FAILED - CorrelationId: {CorrelationId} | Path: {Path} | ClientIP: {ClientIP} | Reason: Empty token",
                correlationId, path, GetClientIpAddress(context));
                
            await WriteUnauthorizedResponse(context, "Empty token provided", correlationId);
            return;
        }

        try
        {
            var principal = ValidateToken(token);
            
            if (principal == null)
            {
                _logger.LogWarning(
                    "AUTH FAILED - CorrelationId: {CorrelationId} | Path: {Path} | ClientIP: {ClientIP} | Reason: Token validation failed",
                    correlationId, path, GetClientIpAddress(context));
                    
                await WriteUnauthorizedResponse(context, "Invalid token", correlationId);
                return;
            }

            // Set the user context
            context.User = principal;
            
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
            var userName = principal.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
            
            _logger.LogInformation(
                "AUTH SUCCESS - CorrelationId: {CorrelationId} | Path: {Path} | UserId: {UserId} | UserName: {UserName}",
                correlationId, path, userId, userName);

            await _next(context);
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning(
                "AUTH FAILED - CorrelationId: {CorrelationId} | Path: {Path} | ClientIP: {ClientIP} | Reason: Token expired",
                correlationId, path, GetClientIpAddress(context));
                
            await WriteUnauthorizedResponse(context, "Token has expired", correlationId);
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex,
                "AUTH FAILED - CorrelationId: {CorrelationId} | Path: {Path} | ClientIP: {ClientIP} | Reason: Token validation error",
                correlationId, path, GetClientIpAddress(context));
                
            await WriteUnauthorizedResponse(context, "Invalid token", correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "AUTH ERROR - CorrelationId: {CorrelationId} | Path: {Path} | ClientIP: {ClientIP} | Reason: Unexpected error during authentication",
                correlationId, path, GetClientIpAddress(context));
                
            await WriteUnauthorizedResponse(context, "Authentication failed", correlationId);
        }
    }

    private bool IsPublicEndpoint(string path)
    {
        // Check exact matches
        if (_publicEndpoints.Contains(path))
            return true;

        // Check for Swagger UI assets
        if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
            return true;

        // Check for static files
        if (path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_content", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, _tokenValidationParameters, out SecurityToken validatedToken);
            
            // Ensure the token is a JWT token
            if (validatedToken is not JwtSecurityToken jwtToken || 
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }

    private async Task WriteUnauthorizedResponse(HttpContext context, string message, string correlationId)
    {
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";

        var errorResponse = new ApiResponse<object>
        {
            Success = false,
            Message = "Unauthorized",
            Errors = new List<string> { message },
            Data = new { CorrelationId = correlationId, Timestamp = DateTime.UtcNow }
        };

        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
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

/// <summary>
/// Extension methods for easy registration of authentication middleware
/// </summary>
public static class TokenAuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseTokenAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TokenAuthenticationMiddleware>();
    }
}
