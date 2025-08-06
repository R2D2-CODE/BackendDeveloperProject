using Domain.Entities;
using Domain.Interfaces;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories;

/// <summary>
/// Thread-safe in-memory implementation of the User repository for demonstration purposes
/// </summary>
public class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<Guid, User> _users = new();
    private readonly ILogger<InMemoryUserRepository>? _logger;

    public InMemoryUserRepository(ILogger<InMemoryUserRepository>? logger = null)
    {
        _logger = logger;
        // Seed with some initial data
        SeedData();
    }

    public Task<IEnumerable<User>> GetAllAsync()
    {
        try
        {
            var users = _users.Values.Where(u => u.IsActive).ToList();
            _logger?.LogInformation("Retrieved {Count} active users", users.Count);
            return Task.FromResult<IEnumerable<User>>(users);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error occurred while retrieving all users");
            throw;
        }
    }

    public Task<User?> GetByIdAsync(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty", nameof(id));

            _users.TryGetValue(id, out var user);
            _logger?.LogInformation("Retrieved user {UserId}: {Found}", id, user != null ? "Found" : "Not Found");
            return Task.FromResult(user);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error occurred while retrieving user {UserId}", id);
            throw;
        }
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be null or empty", nameof(email));

            var user = _users.Values.FirstOrDefault(u => 
                u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            
            _logger?.LogInformation("Retrieved user by email {Email}: {Found}", email, user != null ? "Found" : "Not Found");
            return Task.FromResult(user);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error occurred while retrieving user by email {Email}", email);
            throw;
        }
    }

    public Task<User> CreateAsync(User user)
    {
        try
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (_users.Values.Any(u => u.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"A user with email '{user.Email}' already exists");

            _users.TryAdd(user.Id, user);
            _logger?.LogInformation("Created user {UserId} with email {Email}", user.Id, user.Email);
            return Task.FromResult(user);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error occurred while creating user with email {Email}", user?.Email);
            throw;
        }
    }

    public Task<User> UpdateAsync(User user)
    {
        try
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (!_users.ContainsKey(user.Id))
                throw new KeyNotFoundException($"User with ID '{user.Id}' not found");

            // Check for email conflicts with other users
            var existingUserWithEmail = _users.Values.FirstOrDefault(u => 
                u.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase) && u.Id != user.Id);
            
            if (existingUserWithEmail != null)
                throw new InvalidOperationException($"Another user with email '{user.Email}' already exists");

            user.UpdatedAt = DateTime.UtcNow;
            _users.TryUpdate(user.Id, user, _users[user.Id]);
            _logger?.LogInformation("Updated user {UserId} with email {Email}", user.Id, user.Email);
            return Task.FromResult(user);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error occurred while updating user {UserId}", user?.Id);
            throw;
        }
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty", nameof(id));

            var removed = _users.TryRemove(id, out var removedUser);
            if (removed)
            {
                _logger?.LogInformation("Deleted user {UserId} with email {Email}", id, removedUser?.Email);
            }
            else
            {
                _logger?.LogWarning("Attempted to delete non-existent user {UserId}", id);
            }
            return Task.FromResult(removed);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error occurred while deleting user {UserId}", id);
            throw;
        }
    }

    public Task<bool> ExistsAsync(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
                return Task.FromResult(false);

            var exists = _users.ContainsKey(id);
            _logger?.LogInformation("User {UserId} exists: {Exists}", id, exists);
            return Task.FromResult(exists);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error occurred while checking if user {UserId} exists", id);
            throw;
        }
    }

    private void SeedData()
    {
        var sampleUsers = new List<User>
        {
            new User
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@techhive.com",
                PhoneNumber = "+1-555-0101",
                Department = "Engineering",
                Position = "Senior Software Engineer"
            },
            new User
            {
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@techhive.com",
                PhoneNumber = "+1-555-0102",
                Department = "Human Resources",
                Position = "HR Manager"
            },
            new User
            {
                FirstName = "Mike",
                LastName = "Johnson",
                Email = "mike.johnson@techhive.com",
                PhoneNumber = "+1-555-0103",
                Department = "IT",
                Position = "System Administrator"
            }
        };

        foreach (var user in sampleUsers)
        {
            _users.TryAdd(user.Id, user);
        }
        
        _logger?.LogInformation("Seeded {Count} sample users", sampleUsers.Count);
    }
}
