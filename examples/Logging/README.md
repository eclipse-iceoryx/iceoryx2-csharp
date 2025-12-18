# Logging Example

This example demonstrates how to use the iceoryx2 logging functionality in C#.

## Features

* **Console Logging**: Use the built-in console logger
* **File Logging**: Write logs to a file
* **Custom Logger**: Implement custom log formatting and handling
* **Log Levels**: Control verbosity with log levels (Trace, Debug, Info, Warn,
  Error, Fatal)
* **Environment Variables**: Configure logging via `IOX2_LOG_LEVEL` environment variable

## Running the Examples

### Basic Console Logging

```bash
dotnet run basic
```

This example shows:

* Setting log level from environment variable
* Using the console logger
* Writing messages at different log levels
* Seeing library-generated logs

### Custom Logger with Color

```bash
dotnet run custom
```

This example demonstrates:

* Implementing a custom logger callback
* Adding timestamps and colored output
* Formatting log messages

### File Logging

```bash
dotnet run file
```

This example shows:

* Writing logs to a file (`/tmp/iceoryx2_csharp.log`)
* Viewing library logs in the file

## Log Levels

iceoryx2 supports the following log levels (from most verbose to least):

1. **Trace** - Very detailed debugging information
2. **Debug** - Debugging information
3. **Info** - General informational messages (default)
4. **Warn** - Warning messages
5. **Error** - Error messages
6. **Fatal** - Critical errors

## Environment Variable

Set the log level using the `IOX2_LOG_LEVEL` environment variable:

```bash
# Set to Debug level
export IOX2_LOG_LEVEL=DEBUG
dotnet run basic

# Set to Trace level (most verbose)
export IOX2_LOG_LEVEL=TRACE
dotnet run basic

# Set to Warn level (less verbose)
export IOX2_LOG_LEVEL=WARN
dotnet run basic
```

## API Usage

### Basic Logging

```csharp
using Iceoryx2;

// Use console logger
Log.UseConsoleLogger();

// Set log level
Log.SetLogLevel(LogLevel.Debug);

// Write log message
Log.Write(LogLevel.Info, "MyApp", "Application started");
```

### Environment-based Configuration

```csharp
// Set log level from IOX2_LOG_LEVEL environment variable, default to Info
Log.SetLogLevelFromEnvOrDefault();

// Or with custom default
Log.SetLogLevelFromEnvOr(LogLevel.Debug);
```

### Custom Logger

```csharp
// Set custom logger (can only be called once)
bool success = Log.SetLogger((level, origin, message) =>
{
    Console.WriteLine($"[{level}] {origin}: {message}");
});
```

### File Logger

```csharp
// Write logs to file
Log.UseFileLogger("/tmp/myapp.log");
Log.Write(LogLevel.Info, "MyApp", "This goes to the file");
```

## Notes

* The custom logger can only be set once and must be set before any log messages
  are created
* The built-in loggers (console/file) handle this restriction automatically
* Library-generated logs (from iceoryx2 itself) will use the configured logger
* Origin can be null or empty if not needed
