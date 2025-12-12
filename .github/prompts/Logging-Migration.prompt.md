---
agent: agent
model: GPT-5
tools: ['edit', 'search', 'execute/getTerminalOutput', 'execute/runInTerminal', 'read/terminalLastCommand', 'read/terminalSelection', 'execute/createAndRunTask', 'execute/getTaskOutput', 'execute/runTask', 'kentico.docs.mcp/*', 'usages', 'problems', 'changes', 'todo']
description: "Replace Xperience IEventLogService with .NET ILogger<T>"
---

# Xperience by Kentico Logging Migration - IEventLogService to ILogger<T>

You are an expert C# developer working with Xperience by Kentico v30.11.0+. When
you encounter `IEventLogService` usage in C# code, you should migrate it to the
modern .NET `ILogger<T>` API following the official Xperience by Kentico
documentation and v30.11.0 changelog recommendations.

## Applying this migration

1. Search the code base
2. Note the following scenarios

- When you see `IEventLogService` being injected into constructors
- When you encounter calls to `eventLogService.LogInformation()`,
  `eventLogService.LogWarning()`, `eventLogService.LogError()`, or
  `eventLogService.LogException()`
- When reviewing or refactoring existing Xperience by Kentico code
- When creating new logging functionality in custom code

3. Create a plan to migrate those scenarios according to the requirements below
4. Execute that plan

## Why This Migration is Important

Starting with Xperience by Kentico v30.11.0, the logging implementation has been
modernized to use native .NET `ILogger` as the primary logging interface. This
change:

- Provides better developer experience using existing .NET logging knowledge and
  tools
- Simplifies integration with third-party logging frameworks (Serilog, NLog,
  Aspire dashboard, etc.)
- Supports modern patterns like dependency injection, configuration-based
  filtering, and structured logging
- Maintains full backward compatibility for `IEventLogService` (which now
  internally uses `ILogger`)

However, **Kentico strongly recommends switching to `ILogger<T>` when writing
new logging code and updating existing code when possible**.

## How to Perform the Migration

Follow these steps when you encounter `IEventLogService` usage:

### Step 1: Replace Constructor Dependencies

**Before (deprecated):**

```csharp
using CMS.EventLog;

public class MyService
{
    private readonly IEventLogService eventLogService;

    public MyService(IEventLogService eventLogService)
    {
        this.eventLogService = eventLogService;
    }
}
```

**After (recommended):**

```csharp
using Microsoft.Extensions.Logging;

public class MyService
{
    private readonly ILogger<MyService> logger;

    public MyService(ILogger<MyService> logger)
    {
        this.logger = logger;
    }
}
```

### Step 2: Replace Method Calls

**Before (IEventLogService):**

```csharp
// Information logging
eventLogService.LogInformation("CUSTOM_EVENT", "Description");

// Warning logging
eventLogService.LogWarning("CUSTOM_WARNING", "Warning description");

// Error logging
eventLogService.LogError("CUSTOM_ERROR", "Error description");

// Error with exception
eventLogService.LogException("CUSTOM_EXCEPTION", "Error description", exception);
```

**After (ILogger<T>):**

```csharp
// Information logging with event code
logger.LogInformation(new EventId(0, "CUSTOM_EVENT"), "Description");

// Warning logging with event code
logger.LogWarning(new EventId(0, "CUSTOM_WARNING"), "Warning description");

// Error logging with event code
logger.LogError(new EventId(0, "CUSTOM_ERROR"), "Error description");

// Error with exception and event code
logger.LogError(new EventId(0, "CUSTOM_EXCEPTION"), exception, "Error description");
```

### Step 2.1: Use Message Templates (Structured Logging)

When migrating to `ILogger<T>`, prefer **message templates** instead of string
concatenation or interpolation. A message template is the first `string`
argument passed to a logging method and can contain named **placeholders** in
curly braces (e.g. `{EmailAddress}`, `{UserId}`, `{ElapsedMs}`). Each
placeholder must have a corresponding argument value provided after the
template.

Structured logging benefits:

- Enables log providers (and tools like Application Insights, OpenTelemetry,
  Serilog sinks, etc.) to capture named fields for querying and filtering
- Reduces allocation and avoids building throwaway concatenated strings
- Encourages consistent naming of logged data

Best practices for message templates:

1. Use descriptive PascalCase names without spaces: `{UserId}`, `{ContentId}`
2. Avoid string concatenation: `"User " + userId + " logged in"` ❌
3. Avoid C# string interpolation: `$"User {userId} logged in"` ❌
4. Prefer template placeholders: `"User {UserId} logged in"` ✅
5. Escape literal braces with double braces: `"Value is {{test}}"` → renders
   `Value is {test}`
6. Match the number of placeholders to the number of supplied arguments
7. Do not log sensitive or PII data unless required and reviewed (e.g.
   passwords, secrets, tokens)
8. Keep templates concise; move long details (JSON payloads, etc.) behind higher
   log levels (`Debug`/`Trace`) or attach as separate structured fields

Conversion examples:

**Before (concatenation / interpolation):**

```csharp
logger.LogInformation("Email sent to " + to);
logger.LogWarning($"User {userId} failed validation for page {pageId}");
```

**After (message templates):**

```csharp
logger.LogInformation("Email sent to {EmailAddress}", to);
logger.LogWarning("User {UserId} failed validation for page {PageId}", userId, pageId);
```

Multiple values & formatting:

```csharp
logger.LogInformation(
  new EventId(0, "CONTENT_PUBLISH"),
  "Published content {ContentId} in {ElapsedMs}ms (Version {Version})",
  contentId,
  elapsedMilliseconds,
  versionNumber);
```

Error with exception and template:

```csharp
try
{
  await processor.RunAsync(jobId);
  logger.LogInformation("Job {JobId} completed successfully", jobId);
}
catch (Exception ex)
{
  logger.LogError(ex, "Job {JobId} failed during processing", jobId);
  throw;
}
```

Performance tip: Logging frameworks evaluate arguments only if the log level is
enabled. Avoid expensive synchronous operations (like large object
serialization) inside argument expressions for `Information`+ levels; defer or
guard them.

Consistency checklist for templates:

- Placeholder names reflect domain concepts (`{ContentType}`, `{TagNames}`)
- Errors include identifiers needed for root cause analysis (`{UserId}`,
  `{CorrelationId}`)
- Timing metrics use explicit unit suffix (`{ElapsedMs}` not `{Elapsed}`)
- Booleans use affirmative naming (`{IsCached}`)

Using message templates ensures the Xperience event log and any connected
logging infrastructure store structured fields that can be queried (e.g., find
all `EMAIL_ERROR` events for a specific `{EmailAddress}`).

### Step 3: Understanding Log Level Mapping

Map `IEventLogService` methods to appropriate `ILogger<T>` log levels:

| IEventLogService Method | ILogger<T> Method                 | Event Type in Xperience Event Log |
| ----------------------- | --------------------------------- | --------------------------------- |
| `LogInformation()`      | `logger.LogInformation()`         | Info                              |
| `LogWarning()`          | `logger.LogWarning()`             | Warning                           |
| `LogError()`            | `logger.LogError()`               | Error                             |
| `LogException()`        | `logger.LogError(exception, ...)` | Error                             |
| N/A                     | `logger.LogTrace()`               | Info                              |
| N/A                     | `logger.LogDebug()`               | Info                              |
| N/A                     | `logger.LogCritical()`            | Error                             |

### Step 4: Handle Event Codes and Categories

- **Category**: The `TCategoryName` generic in `ILogger<TCategoryName>` sets the
  logging category (fully qualified type name)
- **Event Code**: Use `EventId` parameter with a `name` property to set the
  event code
- **Source**: The category determines the **Source** value in the Xperience
  event log

**Example:**

```csharp
// Creates events with source "MyNamespace.MyService" and event code "CUSTOM_OPERATION"
public class MyService
{
    private readonly ILogger<MyService> logger;

    public void DoSomething()
    {
        logger.LogInformation(new EventId(0, "CUSTOM_OPERATION"), "Operation completed successfully");
    }
}
```

### Step 5: Consider Configuration Requirements

By default, only events with log level `Warning` or higher from custom code are
written to the Xperience event log. To include lower levels, add configuration
in `appsettings.json`:

```json
{
  "Logging": {
    "XperienceEventLog": {
      "LogLevel": {
        "Default": "Warning",
        "YourNamespace": "Debug" // Include Debug+ events from your namespace
      }
    }
  }
}
```

### Step 6: Utilize Advanced Logging Features

**Interval Policies** (prevent log flooding):

```csharp
// Log only once per application lifetime
logger.LogWithIntervalPolicy(
    LoggingIntervalPolicy.OnlyOnce("unique-identifier"),
    log => log.LogInformation("This will only be logged once"));

// Log at most once every 5 minutes
logger.LogWithIntervalPolicy(
    LoggingIntervalPolicy.OncePerPeriod("rate-limited-event", TimeSpan.FromMinutes(5)),
    log => log.LogWarning("Rate-limited warning"));
```

## Migration Checklist

When performing this migration, ensure you:

1. ✅ Replace `IEventLogService` constructor parameters with `ILogger<T>`
2. ✅ Update using statements (`Microsoft.Extensions.Logging` instead of
   `CMS.EventLog`)
3. ✅ Convert `LogInformation/LogWarning/LogError/LogException` calls to
   appropriate `logger.Log*()` methods
4. ✅ Add `EventId` parameters where event codes are needed
5. ✅ Ensure the generic type `T` in `ILogger<T>` represents the class for
   proper categorization
6. ✅ Review logging configuration in `appsettings.json` if lower log levels are
   needed
7. ✅ Test that events still appear correctly in the Xperience Event Log
   application

## Key Reminders

- **Always Prefer ILogger<T>**: Kentico strongly recommends using `ILogger<T>`
  for all new logging code
- **Backward Compatibility**: Existing `IEventLogService` code continues to work
  but is deprecated
- **Gradual Migration**: `IEventLogService` usage within Xperience libraries
  will be gradually replaced
- **Performance**: Use interval policies for high-frequency events to prevent
  log flooding
- **Structured Logging**: Take advantage of structured logging with parameter
  placeholders like `{EmailAddress}`

## Complete Migration Example

**Before:**

```csharp
using CMS.EventLog;

namespace MyProject.Services
{
    public class EmailService
    {
        private readonly IEventLogService eventLogService;

        public EmailService(IEventLogService eventLogService)
        {
            this.eventLogService = eventLogService;
        }

        public async Task SendEmailAsync(string to, string subject)
        {
            try
            {
                // Send email logic
                eventLogService.LogInformation("EMAIL_SENT", $"Email sent to {to}");
            }
            catch (Exception ex)
            {
                eventLogService.LogException("EMAIL_ERROR", $"Failed to send email to {to}", ex);
                throw;
            }
        }
    }
}
```

**After:**

```csharp
using Microsoft.Extensions.Logging;

namespace MyProject.Services
{
    public class EmailService
    {
        private readonly ILogger<EmailService> logger;

        public EmailService(ILogger<EmailService> logger)
        {
            this.logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject)
        {
            try
            {
                // Send email logic
                logger.LogInformation(new EventId(0, "EMAIL_SENT"), "Email sent to {EmailAddress}", to);
            }
            catch (Exception ex)
            {
                logger.LogError(new EventId(0, "EMAIL_ERROR"), ex, "Failed to send email to {EmailAddress}", to);
                throw;
            }
        }
    }
}
```

Note the use of structured logging with `{EmailAddress}` parameter in the final
example - this is a .NET logging best practice that `ILogger<T>` supports
natively.

Remember: When you encounter `IEventLogService` in any C# code within an
Xperience by Kentico project, apply this migration pattern to modernize the
logging implementation and follow current best practices.
