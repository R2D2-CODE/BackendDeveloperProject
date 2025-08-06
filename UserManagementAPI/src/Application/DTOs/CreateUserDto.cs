using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

/// <summary>
/// Data Transfer Object for creating a new user
/// </summary>
public class CreateUserDto
{
    /// <summary>
    /// User's first name
    /// </summary>
    [Required(ErrorMessage = "First name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters")]
    public required string FirstName { get; set; }

    /// <summary>
    /// User's last name
    /// </summary>
    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters")]
    public required string LastName { get; set; }

    /// <summary>
    /// User's email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public required string Email { get; set; }

    /// <summary>
    /// User's phone number (optional)
    /// </summary>
    [Phone(ErrorMessage = "Invalid phone number format")]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// User's department
    /// </summary>
    [Required(ErrorMessage = "Department is required")]
    [StringLength(100, ErrorMessage = "Department name cannot exceed 100 characters")]
    public required string Department { get; set; }

    /// <summary>
    /// User's position
    /// </summary>
    [Required(ErrorMessage = "Position is required")]
    [StringLength(100, ErrorMessage = "Position name cannot exceed 100 characters")]
    public required string Position { get; set; }
}
