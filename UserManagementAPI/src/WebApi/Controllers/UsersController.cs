using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Controller for managing users in the TechHive Solutions system
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all users from the system
    /// </summary>
    /// <returns>A list of all users</returns>
    /// <response code="200">Returns the list of users</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetAllUsers()
    {
        _logger.LogInformation("Retrieving all users");
        
        var result = await _userService.GetAllUsersAsync();
        
        if (result.Success)
        {
            _logger.LogInformation("Successfully retrieved {Count} users", result.Data?.Count() ?? 0);
            return Ok(result);
        }
        
        _logger.LogError("Failed to retrieve users: {Message}", result.Message);
        return StatusCode(500, result);
    }

    /// <summary>
    /// Retrieves a specific user by their ID
    /// </summary>
    /// <param name="id">The unique identifier of the user</param>
    /// <returns>The user with the specified ID</returns>
    /// <response code="200">Returns the requested user</response>
    /// <response code="404">If the user is not found</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<UserDto?>>> GetUserById(Guid id)
    {
        _logger.LogInformation("Retrieving user with ID: {UserId}", id);
        
        var result = await _userService.GetUserByIdAsync(id);
        
        if (result.Success && result.Data != null)
        {
            _logger.LogInformation("Successfully retrieved user: {UserEmail}", result.Data.Email);
            return Ok(result);
        }
        
        if (!result.Success && result.Message == "User not found")
        {
            _logger.LogWarning("User not found with ID: {UserId}", id);
            return NotFound(result);
        }
        
        _logger.LogError("Failed to retrieve user {UserId}: {Message}", id, result.Message);
        return StatusCode(500, result);
    }

    /// <summary>
    /// Creates a new user in the system
    /// </summary>
    /// <param name="createUserDto">The user data to create</param>
    /// <returns>The newly created user</returns>
    /// <response code="201">Returns the newly created user</response>
    /// <response code="400">If the user data is invalid</response>
    /// <response code="409">If a user with the same email already exists</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] CreateUserDto createUserDto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for user creation");
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            
            var validationResponse = ApiResponse<UserDto>.ErrorResponse("Validation failed", errors);
            return BadRequest(validationResponse);
        }

        _logger.LogInformation("Creating new user with email: {Email}", createUserDto.Email);
        
        var result = await _userService.CreateUserAsync(createUserDto);
        
        if (result.Success)
        {
            _logger.LogInformation("Successfully created user: {UserEmail}", result.Data?.Email);
            return CreatedAtAction(
                nameof(GetUserById), 
                new { id = result.Data?.Id }, 
                result
            );
        }
        
        if (result.Errors.Any(e => e.Contains("email already exists")))
        {
            _logger.LogWarning("Attempted to create user with existing email: {Email}", createUserDto.Email);
            return Conflict(result);
        }
        
        _logger.LogError("Failed to create user: {Message}", result.Message);
        return StatusCode(500, result);
    }

    /// <summary>
    /// Updates an existing user's information
    /// </summary>
    /// <param name="id">The unique identifier of the user to update</param>
    /// <param name="updateUserDto">The updated user data</param>
    /// <returns>The updated user</returns>
    /// <response code="200">Returns the updated user</response>
    /// <response code="400">If the user data is invalid</response>
    /// <response code="404">If the user is not found</response>
    /// <response code="409">If the email conflicts with another user</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(Guid id, [FromBody] UpdateUserDto updateUserDto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for user update");
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            
            var validationResponse = ApiResponse<UserDto>.ErrorResponse("Validation failed", errors);
            return BadRequest(validationResponse);
        }

        _logger.LogInformation("Updating user with ID: {UserId}", id);
        
        var result = await _userService.UpdateUserAsync(id, updateUserDto);
        
        if (result.Success)
        {
            _logger.LogInformation("Successfully updated user: {UserEmail}", result.Data?.Email);
            return Ok(result);
        }
        
        if (result.Message == "User not found")
        {
            _logger.LogWarning("Attempted to update non-existent user: {UserId}", id);
            return NotFound(result);
        }
        
        if (result.Errors.Any(e => e.Contains("email already exists")))
        {
            _logger.LogWarning("Attempted to update user with conflicting email: {Email}", updateUserDto.Email);
            return Conflict(result);
        }
        
        _logger.LogError("Failed to update user {UserId}: {Message}", id, result.Message);
        return StatusCode(500, result);
    }

    /// <summary>
    /// Deletes a user from the system
    /// </summary>
    /// <param name="id">The unique identifier of the user to delete</param>
    /// <returns>Confirmation of deletion</returns>
    /// <response code="200">Returns confirmation of successful deletion</response>
    /// <response code="404">If the user is not found</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(Guid id)
    {
        _logger.LogInformation("Deleting user with ID: {UserId}", id);
        
        var result = await _userService.DeleteUserAsync(id);
        
        if (result.Success)
        {
            _logger.LogInformation("Successfully deleted user: {UserId}", id);
            return Ok(result);
        }
        
        if (result.Message == "User not found")
        {
            _logger.LogWarning("Attempted to delete non-existent user: {UserId}", id);
            return NotFound(result);
        }
        
        _logger.LogError("Failed to delete user {UserId}: {Message}", id, result.Message);
        return StatusCode(500, result);
    }
}
