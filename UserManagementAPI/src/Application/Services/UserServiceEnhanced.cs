using Application.DTOs;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services;

/// <summary>
/// Enhanced service implementation for User business logic with improved error handling
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, IMapper mapper, ILogger<UserService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ApiResponse<IEnumerable<UserDto>>> GetAllUsersAsync()
    {
        try
        {
            _logger.LogInformation("Retrieving all users from repository");
            
            var users = await _userRepository.GetAllAsync();
            var userDtos = _mapper.Map<IEnumerable<UserDto>>(users);
            
            _logger.LogInformation("Successfully retrieved {Count} users", userDtos.Count());
            
            return ApiResponse<IEnumerable<UserDto>>.SuccessResponse(
                userDtos, 
                $"Retrieved {userDtos.Count()} users successfully"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve all users");
            return ApiResponse<IEnumerable<UserDto>>.ErrorResponse(
                "Failed to retrieve users", 
                new List<string> { "An error occurred while retrieving users. Please try again." }
            );
        }
    }

    public async Task<ApiResponse<UserDto?>> GetUserByIdAsync(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
            {
                _logger.LogWarning("Attempt to retrieve user with empty GUID");
                return ApiResponse<UserDto?>.ErrorResponse(
                    "Invalid user ID", 
                    new List<string> { "User ID cannot be empty" }
                );
            }

            _logger.LogInformation("Retrieving user with ID: {UserId}", id);
            
            var user = await _userRepository.GetByIdAsync(id);
            
            if (user == null)
            {
                _logger.LogWarning("User not found with ID: {UserId}", id);
                return ApiResponse<UserDto?>.ErrorResponse("User not found");
            }

            var userDto = _mapper.Map<UserDto>(user);
            _logger.LogInformation("Successfully retrieved user: {UserEmail}", user.Email);
            
            return ApiResponse<UserDto?>.SuccessResponse(userDto, "User retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve user with ID: {UserId}", id);
            return ApiResponse<UserDto?>.ErrorResponse(
                "Failed to retrieve user", 
                new List<string> { "An error occurred while retrieving the user. Please try again." }
            );
        }
    }

    public async Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserDto createUserDto)
    {
        try
        {
            if (createUserDto == null)
            {
                _logger.LogWarning("Attempt to create user with null data");
                throw new ArgumentNullException(nameof(createUserDto));
            }

            _logger.LogInformation("Creating new user with email: {Email}", createUserDto.Email);

            // Additional business validation
            if (string.IsNullOrWhiteSpace(createUserDto.Email))
            {
                return ApiResponse<UserDto>.ErrorResponse(
                    "User creation failed", 
                    new List<string> { "Email is required and cannot be empty" }
                );
            }

            // Check if user with email already exists
            var existingUser = await _userRepository.GetByEmailAsync(createUserDto.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Attempt to create user with existing email: {Email}", createUserDto.Email);
                return ApiResponse<UserDto>.ErrorResponse(
                    "User creation failed", 
                    new List<string> { "A user with this email already exists" }
                );
            }

            var user = _mapper.Map<User>(createUserDto);
            
            if (user == null)
            {
                _logger.LogError("AutoMapper failed to map CreateUserDto to User entity");
                return ApiResponse<UserDto>.ErrorResponse(
                    "User creation failed", 
                    new List<string> { "Failed to process user data" }
                );
            }

            var createdUser = await _userRepository.CreateAsync(user);
            var userDto = _mapper.Map<UserDto>(createdUser);

            _logger.LogInformation("Successfully created user: {UserEmail} with ID: {UserId}", 
                createdUser.Email, createdUser.Id);

            return ApiResponse<UserDto>.SuccessResponse(userDto, "User created successfully");
        }
        catch (ArgumentNullException)
        {
            throw; // Re-throw argument null exceptions
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while creating user");
            return ApiResponse<UserDto>.ErrorResponse(
                "User creation failed", 
                new List<string> { ex.Message }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating user with email: {Email}", 
                createUserDto?.Email);
            return ApiResponse<UserDto>.ErrorResponse(
                "Failed to create user", 
                new List<string> { "An unexpected error occurred. Please try again." }
            );
        }
    }

    public async Task<ApiResponse<UserDto>> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto)
    {
        try
        {
            if (id == Guid.Empty)
            {
                _logger.LogWarning("Attempt to update user with empty GUID");
                return ApiResponse<UserDto>.ErrorResponse(
                    "Invalid user ID", 
                    new List<string> { "User ID cannot be empty" }
                );
            }

            if (updateUserDto == null)
            {
                _logger.LogWarning("Attempt to update user with null data");
                throw new ArgumentNullException(nameof(updateUserDto));
            }

            _logger.LogInformation("Updating user with ID: {UserId}", id);

            var existingUser = await _userRepository.GetByIdAsync(id);
            if (existingUser == null)
            {
                _logger.LogWarning("Attempt to update non-existent user: {UserId}", id);
                return ApiResponse<UserDto>.ErrorResponse("User not found");
            }

            // Check if email is being changed and if another user already has this email
            if (!existingUser.Email.Equals(updateUserDto.Email, StringComparison.OrdinalIgnoreCase))
            {
                var userWithEmail = await _userRepository.GetByEmailAsync(updateUserDto.Email);
                if (userWithEmail != null && userWithEmail.Id != id)
                {
                    _logger.LogWarning("Attempt to update user {UserId} with existing email: {Email}", 
                        id, updateUserDto.Email);
                    return ApiResponse<UserDto>.ErrorResponse(
                        "User update failed", 
                        new List<string> { "Another user with this email already exists" }
                    );
                }
            }

            // Map the update DTO to the existing user
            _mapper.Map(updateUserDto, existingUser);
            existingUser.UpdatedAt = DateTime.UtcNow;

            var updatedUser = await _userRepository.UpdateAsync(existingUser);
            var userDto = _mapper.Map<UserDto>(updatedUser);

            _logger.LogInformation("Successfully updated user: {UserEmail} with ID: {UserId}", 
                updatedUser.Email, updatedUser.Id);

            return ApiResponse<UserDto>.SuccessResponse(userDto, "User updated successfully");
        }
        catch (ArgumentNullException)
        {
            throw; // Re-throw argument null exceptions
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found during update: {UserId}", id);
            return ApiResponse<UserDto>.ErrorResponse("User not found");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while updating user: {UserId}", id);
            return ApiResponse<UserDto>.ErrorResponse(
                "User update failed", 
                new List<string> { ex.Message }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating user: {UserId}", id);
            return ApiResponse<UserDto>.ErrorResponse(
                "Failed to update user", 
                new List<string> { "An unexpected error occurred. Please try again." }
            );
        }
    }

    public async Task<ApiResponse<bool>> DeleteUserAsync(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
            {
                _logger.LogWarning("Attempt to delete user with empty GUID");
                return ApiResponse<bool>.ErrorResponse(
                    "Invalid user ID", 
                    new List<string> { "User ID cannot be empty" }
                );
            }

            _logger.LogInformation("Deleting user with ID: {UserId}", id);

            var exists = await _userRepository.ExistsAsync(id);
            if (!exists)
            {
                _logger.LogWarning("Attempt to delete non-existent user: {UserId}", id);
                return ApiResponse<bool>.ErrorResponse("User not found");
            }

            var deleted = await _userRepository.DeleteAsync(id);
            
            if (deleted)
            {
                _logger.LogInformation("Successfully deleted user: {UserId}", id);
                return ApiResponse<bool>.SuccessResponse(true, "User deleted successfully");
            }
            else
            {
                _logger.LogError("Failed to delete user: {UserId}", id);
                return ApiResponse<bool>.ErrorResponse("Failed to delete user");
            }
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while deleting user: {UserId}", id);
            return ApiResponse<bool>.ErrorResponse(
                "Invalid user ID", 
                new List<string> { ex.Message }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting user: {UserId}", id);
            return ApiResponse<bool>.ErrorResponse(
                "Failed to delete user", 
                new List<string> { "An unexpected error occurred. Please try again." }
            );
        }
    }
}
