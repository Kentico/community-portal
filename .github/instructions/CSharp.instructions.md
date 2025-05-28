---
applyTo: "**/*.cs"
---

# C# Code Conventions

## Model Conventions

1. View Models should:
   - Use nullable property initializers
   - Default collections to empty arrays using collection expressions
   - Use record types for DTOs
   - Use init-only properties where appropriate
   - Include XML documentation for complex models
   - Follow consistent naming (ViewModel suffix for view models)

Example:

```csharp
public class ExampleViewModel
{
    public string Title { get; set; } = "";
    public IReadOnlyList<string> Items { get; } = [];
}

public record ExampleRequest(string Name, int Value);
```

## Service Classes

1. Services should:
   - Use constructor-based dependency injection with primary constructor syntax
   - Be registered with appropriate lifetimes
   - Have clear interface definitions
   - Follow single responsibility principle
   - Use async/await for asynchronous operations
   - Include XML documentation for public APIs

Example:

```csharp
public class ExampleService(
    ILogger<ExampleService> logger,
    IConfiguration config)
{
    private readonly ILogger<ExampleService> logger = logger;
    private readonly IConfiguration config = config;
}
```

## Error Handling

1. Error handling should:
   - Use ModelState for validation errors
   - Return appropriate HTTP status codes
   - Log errors using ILogger or IEventLogService
   - Use custom exceptions for domain-specific errors
   - Include meaningful error messages

## File Organization

1. Files should be organized:
   - By feature area in Features folder
   - Components in Components folder
   - Common infrastructure in Infrastructure folder
   - Feature-specific models with their features
   - One primary class per file
   - Use nested namespaces matching folder structure

## Dependency Injection

1. Dependencies should:
   - Be registered in feature-specific extension methods
   - Use constructor injection
   - Follow interface segregation principle
   - Use appropriate service lifetimes
   - Be mockable for testing

## Code Style

1. General:
   - Use C# latest features (e.g., primary constructors, collection expressions)
   - Prefer async/await over Task continuations
   - Use expression-bodied members where appropriate
   - Validate parameters early
   - Use nullable reference types
   - Use constants for magic strings/numbers
   - Initialize properties with default values
   - Prioritize using `Result<T>` monad from CSharpFunctionalExtensions library
     over throwing exceptions
   - Prioritize using `Maybe<T>` monad from CSharpFunctionalExtensions library
     over C# nullable reference types

## Security

1. Security practices:
   - Always use [ValidateAntiForgeryToken] for POST actions
   - Validate user permissions explicitly
   - Use proper authorization attributes
   - Sanitize user input
   - Use HTTPS
   - Follow OWASP security guidelines

## Testing

1. Tests should:
   - Be organized alongside the code they test
   - Follow Arrange-Act-Assert pattern
   - Use meaningful test names
   - Mock external dependencies
   - Test both success and failure paths
   - Use appropriate test categories
