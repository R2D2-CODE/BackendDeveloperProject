# User Management API

A comprehensive RESTful API for managing users in TechHive Solutions internal tools, built with .NET Core 9 using clean architecture principles and best practices.

## 🏗️ Architecture

This project follows **Clean Architecture** principles with clear separation of concerns:

```
UserManagementAPI/
├── src/
│   ├── Domain/              # Core business logic and entities
│   │   ├── Entities/        # Domain entities
│   │   └── Interfaces/      # Domain interfaces
│   ├── Application/         # Business logic and services
│   │   ├── Services/        # Business services
│   │   ├── DTOs/           # Data Transfer Objects
│   │   └── Mappings/       # AutoMapper profiles
│   ├── Infrastructure/      # Data access and external concerns
│   │   └── Repositories/   # Data repositories
│   └── WebApi/             # API controllers and configuration
│       └── Controllers/    # API endpoints
├── tests/                  # Unit and integration tests (future)
└── requests.http          # HTTP client requests for testing
```

## 🚀 Features

- **Complete CRUD Operations**: Create, Read, Update, Delete users
- **Clean Architecture**: Separated layers with clear dependencies
- **Comprehensive Validation**: Input validation with detailed error messages
- **Swagger Documentation**: Interactive API documentation
- **Structured Logging**: Using Serilog with file and console output
- **Error Handling**: Consistent error responses and status codes
- **AutoMapper Integration**: Automatic object mapping
- **Health Checks**: API health monitoring endpoint
- **HTTP Client Testing**: Ready-to-use requests.http file

## 🛠️ Design Patterns Used

1. **Repository Pattern**: Abstracts data access layer
2. **Service Layer Pattern**: Encapsulates business logic
3. **Dependency Injection**: Loose coupling between components
4. **DTO Pattern**: Data transfer between layers
5. **Response Wrapper Pattern**: Consistent API responses
6. **Builder Pattern**: Used in Program.cs configuration

## 📋 API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/users` | Retrieve all users |
| GET | `/api/users/{id}` | Retrieve user by ID |
| POST | `/api/users` | Create new user |
| PUT | `/api/users/{id}` | Update existing user |
| DELETE | `/api/users/{id}` | Delete user by ID |
| GET | `/health` | Health check endpoint |
| GET | `/` | API documentation redirect |

## 🏃‍♂️ Getting Started

### Prerequisites
- .NET 9.0 SDK
- Visual Studio Code with REST Client extension (for testing)

### Running the API

1. Navigate to the WebApi project directory:
```bash
cd src/WebApi
```

2. Restore dependencies:
```bash
dotnet restore
```

3. Run the application:
```bash
dotnet run
```

4. The API will be available at:
   - HTTPS: `https://localhost:7001`
   - HTTP: `http://localhost:5001`
   - Swagger UI: `https://localhost:7001` (redirects to Swagger)

### Testing with HTTP Client

1. Open `requests.http` in VS Code
2. Install the "REST Client" extension if not already installed
3. Click "Send Request" above each request to test the API
4. Replace placeholder GUIDs with actual user IDs from responses

## 🧪 Sample Data

The API comes with pre-seeded sample users:
- John Doe (Engineering - Senior Software Engineer)
- Jane Smith (Human Resources - HR Manager)  
- Mike Johnson (IT - System Administrator)

## 📝 Request/Response Examples

### Create User
```http
POST /api/users
Content-Type: application/json

{
  "firstName": "Alice",
  "lastName": "Johnson",
  "email": "alice.johnson@techhive.com",
  "phoneNumber": "+1-555-0104",
  "department": "Marketing",
  "position": "Marketing Manager"
}
```

### Response Format
```json
{
  "success": true,
  "message": "User created successfully",
  "data": {
    "id": "guid-here",
    "firstName": "Alice",
    "lastName": "Johnson",
    "email": "alice.johnson@techhive.com",
    "phoneNumber": "+1-555-0104",
    "department": "Marketing",
    "position": "Marketing Manager",
    "createdAt": "2025-08-06T...",
    "updatedAt": "2025-08-06T...",
    "isActive": true,
    "fullName": "Alice Johnson"
  },
  "errors": []
}
```

## 🔍 Logging

Logs are written to:
- Console output
- File: `logs/usermanagement-{date}.txt`

Log levels are configured for optimal development and production use.

## 🏆 Best Practices Implemented

- **Clean Architecture**: Clear separation of concerns
- **SOLID Principles**: Single responsibility, dependency inversion, etc.
- **Async/Await**: Proper asynchronous programming
- **Input Validation**: Comprehensive model validation
- **Error Handling**: Consistent error responses
- **Logging**: Structured logging with Serilog
- **Documentation**: XML comments for Swagger generation
- **HTTP Status Codes**: Proper status code usage
- **RESTful Design**: Following REST conventions

## 🔮 Future Enhancements

- Database integration (Entity Framework Core)
- Unit and integration tests
- Authentication and authorization
- Pagination for user lists
- Advanced filtering and searching
- Rate limiting
- Caching strategies
- Docker containerization

## 🤝 Microsoft Copilot Assistance

This project was developed with significant assistance from Microsoft Copilot, which helped with:

1. **Project Structure**: Scaffolding the clean architecture layout
2. **Boilerplate Code**: Generating repetitive code patterns
3. **Best Practices**: Implementing industry-standard patterns
4. **Documentation**: Creating comprehensive XML documentation
5. **Error Handling**: Implementing robust error handling strategies
6. **Configuration**: Setting up proper dependency injection and logging
7. **Testing Setup**: Creating comprehensive HTTP client tests

Copilot significantly accelerated development while ensuring adherence to best practices and clean code principles.
