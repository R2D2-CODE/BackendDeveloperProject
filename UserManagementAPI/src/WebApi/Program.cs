using Application.Mappings;
using Application.Services;
using Application.Validators;
using Domain.Interfaces;
using FluentValidation;
using FluentValidation.AspNetCore;
using Infrastructure.Repositories;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with enhanced settings
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "UserManagementAPI")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/usermanagement-.txt", 
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Customize model validation error responses
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            
            var response = new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(
                Application.DTOs.ApiResponse<object>.ErrorResponse("Validation failed", errors));
            
            return response;
        };
    });

// Configure AutoMapper
builder.Services.AddAutoMapper(typeof(UserMappingProfile));

// Configure FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserDtoValidator>();

// Register repositories with proper lifetime
builder.Services.AddScoped<IUserRepository, InMemoryUserRepository>();

// Register services
builder.Services.AddScoped<IUserService, UserService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "User Management API",
        Description = "A comprehensive API for managing users in TechHive Solutions internal tools with enhanced security and error handling",
        Contact = new OpenApiContact
        {
            Name = "TechHive Solutions Development Team",
            Email = "dev@techhive.com"
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Set the comments path for the Swagger JSON and UI.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
    
    // Add security definitions for future authentication
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
});

// Add CORS policy with enhanced security
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentCorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3001", "http://localhost:8080")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
    
    options.AddPolicy("ProductionCorsPolicy", policy =>
    {
        policy.WithOrigins("https://techhive.com", "https://api.techhive.com")
              .WithMethods("GET", "POST", "PUT", "DELETE")
              .WithHeaders("Content-Type", "Authorization")
              .AllowCredentials();
    });
});

// Add health checks with custom checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
    .AddCheck<Infrastructure.HealthChecks.MemoryHealthCheck>("memory");

// Add rate limiting (for future implementation)
builder.Services.AddMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline.

// Add global exception handling middleware first
app.UseMiddleware<GlobalExceptionMiddleware>();

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    
    if (context.Request.IsHttps)
    {
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    }
    
    await next();
});

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "User Management API v1");
    c.RoutePrefix = string.Empty; // Set Swagger UI at apps root
    c.DisplayRequestDuration();
    c.EnableTryItOutByDefault();
    c.DocumentTitle = "User Management API Documentation";
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseCors("DevelopmentCorsPolicy");
}
else
{
    app.UseHsts();
    app.UseCors("ProductionCorsPolicy");
}

app.UseHttpsRedirection();

// Add request logging
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault());
    };
});

app.UseAuthorization();

app.MapControllers();

// Add health check endpoint with detailed response
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions()
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                description = x.Value.Description,
                duration = x.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

// Add API information endpoint
app.MapGet("/info", () => new
{
    Version = "1.0.0",
    Environment = app.Environment.EnvironmentName,
    MachineName = Environment.MachineName,
    ProcessId = Environment.ProcessId,
    Timestamp = DateTime.UtcNow
}).WithTags("Information");

// Add a simple root endpoint
app.MapGet("/", () => "User Management API is running. Navigate to /swagger for API documentation.")
    .WithTags("Information");

try
{
    Log.Information("Starting User Management API v1.0.0");
    Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
