namespace Application.DTOs;

/// <summary>
/// Data Transfer Object for User responses
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public string? PhoneNumber { get; set; }
    public required string Department { get; set; }
    public required string Position { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public string FullName { get; set; } = string.Empty;
}
