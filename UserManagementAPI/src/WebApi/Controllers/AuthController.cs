using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace WebApi.Controllers;

/// <summary>
/// Authentication controller for token generation (for testing purposes)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(ILogger<AuthController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Generate a JWT token for testing purposes
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT token for API access</returns>
    [HttpPost("login")]
    public ActionResult<ApiResponse<object>> Login([FromBody] LoginRequest request)
    {
        try
        {
            // Simple validation for demonstration (in production, validate against database)
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                _logger.LogWarning("Login attempt with missing credentials from IP: {ClientIP}", GetClientIpAddress());
                return BadRequest(ApiResponse<object>.ErrorResponse("Username and password are required"));
            }

            // Demo user credentials (in production, validate against database)
            var validUsers = new Dictionary<string, (string Password, string Role, string UserId)>
            {
                ["admin"] = ("admin123", "Administrator", "admin-001"),
                ["user"] = ("user123", "User", "user-001"),
                ["techhive"] = ("techhive2024", "Manager", "manager-001")
            };

            if (!validUsers.TryGetValue(request.Username, out var userInfo) || userInfo.Password != request.Password)
            {
                _logger.LogWarning("Failed login attempt for username: {Username} from IP: {ClientIP}", 
                    request.Username, GetClientIpAddress());
                return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid username or password"));
            }

            var token = GenerateJwtToken(userInfo.UserId, request.Username, userInfo.Role);
            
            _logger.LogInformation("Successful login for username: {Username} from IP: {ClientIP}", 
                request.Username, GetClientIpAddress());

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                Token = token,
                TokenType = "Bearer",
                ExpiresIn = 3600, // 1 hour
                Username = request.Username,
                Role = userInfo.Role
            }, "Login successful"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login process for username: {Username}", request.Username);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred during login"));
        }
    }

    /// <summary>
    /// Get current user information (requires authentication)
    /// </summary>
    /// <returns>Current user details</returns>
    [HttpGet("me")]
    public ActionResult<ApiResponse<object>> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                UserId = userId,
                Username = username,
                Role = role,
                IsAuthenticated = true
            }, "User information retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user information");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred retrieving user information"));
        }
    }

    private string GenerateJwtToken(string userId, string username, string role)
    {
        var secretKey = _configuration["JwtSettings:SecretKey"] ?? "TechHive-Super-Secret-Key-For-JWT-Tokens-2024!";
        var issuer = _configuration["JwtSettings:Issuer"] ?? "TechHive.UserManagementAPI";
        var audience = _configuration["JwtSettings:Audience"] ?? "TechHive.Users";
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GetClientIpAddress()
    {
        var xForwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            return xForwardedFor.Split(',')[0].Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}

/// <summary>
/// Login request model
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Username for authentication
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password for authentication
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
