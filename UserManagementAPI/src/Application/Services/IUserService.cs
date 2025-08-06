using Application.DTOs;

namespace Application.Services;

/// <summary>
/// Service interface for User business logic
/// </summary>
public interface IUserService
{
    Task<ApiResponse<IEnumerable<UserDto>>> GetAllUsersAsync();
    Task<ApiResponse<UserDto?>> GetUserByIdAsync(Guid id);
    Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserDto createUserDto);
    Task<ApiResponse<UserDto>> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto);
    Task<ApiResponse<bool>> DeleteUserAsync(Guid id);
}
