using FluentValidation;
using Application.DTOs;

namespace Application.Validators;

/// <summary>
/// Enhanced validator for CreateUserDto with business rules
/// </summary>
public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .Length(2, 50).WithMessage("First name must be between 2 and 50 characters")
            .Matches(@"^[a-zA-Z\s\-'\.]+$").WithMessage("First name can only contain letters, spaces, hyphens, apostrophes, and periods");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .Length(2, 50).WithMessage("Last name must be between 2 and 50 characters")
            .Matches(@"^[a-zA-Z\s\-'\.]+$").WithMessage("Last name can only contain letters, spaces, hyphens, apostrophes, and periods");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .Must(BeValidBusinessEmail).WithMessage("Email must be from a valid business domain")
            .MaximumLength(254).WithMessage("Email cannot exceed 254 characters");

        RuleFor(x => x.PhoneNumber)
            .Must(BeValidPhoneNumber).WithMessage("Phone number must be in format +1-XXX-XXX-XXXX")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.Department)
            .NotEmpty().WithMessage("Department is required")
            .Length(2, 100).WithMessage("Department must be between 2 and 100 characters")
            .Must(BeValidDepartment).WithMessage("Department must be a valid business department");

        RuleFor(x => x.Position)
            .NotEmpty().WithMessage("Position is required")
            .Length(2, 100).WithMessage("Position must be between 2 and 100 characters");
    }

    private static bool BeValidBusinessEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return false;
        
        // Business email validation - must contain @ and valid domain
        var validDomains = new[] { "techhive.com", "company.com", "business.org", "corp.net" };
        var domain = email.Split('@').LastOrDefault()?.ToLowerInvariant();
        
        // For demo purposes, allow any .com, .org, .net domain
        return domain?.EndsWith(".com") == true || 
               domain?.EndsWith(".org") == true || 
               domain?.EndsWith(".net") == true ||
               validDomains.Contains(domain);
    }

    private static bool BeValidPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber)) return true; // Optional field
        
        // US phone number format: +1-XXX-XXX-XXXX
        return System.Text.RegularExpressions.Regex.IsMatch(
            phoneNumber, 
            @"^\+1-\d{3}-\d{3}-\d{4}$"
        );
    }

    private static bool BeValidDepartment(string department)
    {
        if (string.IsNullOrEmpty(department)) return false;
        
        var validDepartments = new[]
        {
            "Engineering", "Human Resources", "IT", "Marketing", "Sales", 
            "Finance", "Operations", "Legal", "Customer Service", "Research and Development",
            "Quality Assurance", "Product Management", "Design", "Security", "Administration"
        };
        
        return validDepartments.Contains(department, StringComparer.OrdinalIgnoreCase);
    }
}
