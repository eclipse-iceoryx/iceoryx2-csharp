# Microsoft.Extensions.Logging Integration Example

This example demonstrates how to integrate iceoryx2's internal logging with the
Microsoft.Extensions.Logging framework, allowing you to see iceoryx2's debug
logs through your existing logging infrastructure.

> **Note:** Starting with iceoryx2 v0.8.0, the console logger is enabled by
> default. The integration examples in this project use `SetLogger()` to
> redirect logs to Microsoft.Extensions.Logging, which continues to work
> as before.

## Features

The example showcases **4 different logging approaches**:

1. **Console Logging** - Basic Microsoft.Extensions.Logging console output
2. **Serilog Integration** - Structured logging with Serilog
3. **Dependency Injection** - ASP.NET Core style DI pattern
4. **Custom Logger** - Color-coded custom logger callback

## Why Use This?

* **Better Debugging**: See internal iceoryx2 logs alongside your application logs
* **Unified Logging**: Use the same logging infrastructure for both your app and
  iceoryx2
* **Flexibility**: Works with any Microsoft.Extensions.Logging provider
  (Serilog, NLog, Application Insights, etc.)
* **Structured Logging**: Get structured log data with scopes and context

## Running the Examples

```bash
# Run all examples in sequence
dotnet run --framework net9.0

# Or run a specific example
dotnet run --framework net9.0 -- console    # Console logging only
dotnet run --framework net9.0 -- serilog    # Serilog structured logging
dotnet run --framework net9.0 -- di         # Dependency injection pattern
dotnet run --framework net9.0 -- custom     # Color-coded custom logger
```

## Example 1: Console Logging

```csharp
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .SetMinimumLevel(MsLogLevel.Trace)
        .AddConsole(options =>
        {
            options.IncludeScopes = true;
            options.TimestampFormat = "HH:mm:ss ";
        });
});

// Integrate iceoryx2 logging
Iox2LoggingExtensions.UseExtensionsLogging(loggerFactory, options =>
{
    options.LogLevel = Iox2LogLevel.Debug;
    options.CategoryName = "Iceoryx2";
});
```

**Output:**

```text
15:32:45 trce: Iceoryx2[0] => [ipc::shm::named_concept] open memory "iox2_e5a7cad39de72e85eda95946a69f2fb5_service" with size 80
15:32:45 dbug: Iceoryx2[0] => [service::dynamic_config] open dynamic service information of "my_demo_service"
```

## Example 2: Serilog Structured Logging

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Trace()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Scope} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddSerilog();
});

Iox2LoggingExtensions.UseExtensionsLogging(loggerFactory, options =>
{
    options.LogLevel = Iox2LogLevel.Debug;
    options.CategoryName = "MyApp.Iceoryx2";
});
```

**Output:**

```text
[15:34:12 TRC] => [ipc::shm::named_concept] open memory "iox2_e5a7cad39de72e85eda95946a69f2fb5_service" with size 80
[15:34:12 DBG] => [service::dynamic_config] open dynamic service information of "my_demo_service"
```

## Example 3: Dependency Injection Pattern

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add iceoryx2 logging integration
builder.Services.AddIceoryx2Logging(options =>
{
    options.LogLevel = Iox2LogLevel.Debug;
    options.CategoryName = "MyApp.Iceoryx2";
});

var app = builder.Build();

// Initialize iceoryx2 logging from DI container
var loggingInitializer = app.Services.GetRequiredService<IIox2LoggingInitializer>();
loggingInitializer.Initialize();
```

**Use Case**: Perfect for ASP.NET Core applications or any app using Microsoft's
DI container.

## Example 4: Custom Logger with Color Coding

```csharp
Iox2Log.SetLogger((level, origin, message) =>
{
    Console.ForegroundColor = level switch
    {
        Iox2LogLevel.Trace => ConsoleColor.Gray,
        Iox2LogLevel.Debug => ConsoleColor.Cyan,
        Iox2LogLevel.Info => ConsoleColor.White,
        Iox2LogLevel.Warn => ConsoleColor.Yellow,
        Iox2LogLevel.Error => ConsoleColor.Red,
        Iox2LogLevel.Fatal => ConsoleColor.Magenta,
        _ => ConsoleColor.White
    };
    
    Console.WriteLine($"[{timestamp}] {level} [{origin}] {message}");
    Console.ForegroundColor = originalColor;
});

Iox2Log.SetLogLevel(Iox2LogLevel.Debug);
```

**Output**: Color-coded logs for easy visual scanning in the console.

## Log Levels

The integration maps between iceoryx2 and Microsoft.Extensions.Logging log levels:

| iceoryx2 | Microsoft.Extensions.Logging |
|----------|------------------------------|
| `Trace`  | `Trace`                      |
| `Debug`  | `Debug`                      |
| `Info`   | `Information`                |
| `Warn`   | `Warning`                    |
| `Error`  | `Error`                      |
| `Fatal`  | `Critical`                   |

## Integration in Your Project

### 1. Add Package References

```xml
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.0" />
<PackageReference Include="Serilog.Extensions.Logging" Version="9.0.0" /> <!-- Optional: for Serilog -->
```

### 2. Use the Extension

```csharp
using Iceoryx2.Extensions;
using Iox2LogLevel = Iceoryx2.LogLevel;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

// Option A: Direct integration
Iox2LoggingExtensions.UseExtensionsLogging(loggerFactory, options =>
{
    options.LogLevel = Iox2LogLevel.Debug;
    options.CategoryName = "Iceoryx2";
});

// Option B: Dependency injection
services.AddIceoryx2Logging(options =>
{
    options.LogLevel = Iox2LogLevel.Debug;
});
```

## Benefits

* **Debugging Made Easy**: See exactly what iceoryx2 is doing internally
* **Production Monitoring**: Route iceoryx2 logs to your existing logging
  pipeline (Application Insights, Elasticsearch, etc.)
* **Structured Data**: Use scopes and structured logging for better log analysis
* **Consistency**: Same logging format and infrastructure for your entire application

## Requirements

* .NET 8.0 or later
* Microsoft.Extensions.Logging.Abstractions 8.0.0+
* Microsoft.Extensions.DependencyInjection.Abstractions 8.0.0+ (for DI support)

## See Also

* [iceoryx2 C# Bindings Documentation](../../README.md)
* [Microsoft.Extensions.Logging Documentation](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging)
* [Serilog Documentation](https://serilog.net/)
