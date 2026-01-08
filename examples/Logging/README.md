# Logging Example

This example demonstrates how to use the iceoryx2 logging functionality in C#.

## Logging Backend Changes in v0.8.0

Starting with iceoryx2 v0.8.0, the logging backend has changed:

* **Console logger is now enabled by default** - No explicit initialization required
* **File logger requires rebuilding iceoryx2** with specific feature flags
* **Custom loggers** can be set at runtime using `SetLogger()`

### Available Loggers (via feature flags on iceoryx2-loggers crate)

1. **console** - outputs log messages to the console (default)
2. **buffer** - outputs log messages to a buffer
3. **file** - outputs log messages to a file
4. **log** - utilizes the `log` crate
5. **tracing** - utilizes the `tracing` crate

### Enabling File Logger

To use the native file logger, rebuild iceoryx2 with:

```bash
cargo build --package iceoryx2-ffi-c --features iceoryx2-loggers/std --features iceoryx2-loggers/file --no-default-features --release
```

Alternatively, use a custom logger callback (demonstrated in this example).

## Features

* **Console Logging**: Enabled by default in iceoryx2 v0.8.0+
* **Custom File Logging**: Implement file logging via custom logger callback
* **Custom Logger**: Implement custom log formatting and handling
* **Log Levels**: Control verbosity with log levels (Trace, Debug, Info, Warn,
  Error, Fatal)
* **Environment Variables**: Configure logging via `IOX2_LOG_LEVEL` environment variable

## Running the Examples

### Basic Console Logging

```bash
dotnet run --framework net9.0 -- basic
```

This example shows:

* Setting log level from environment variable
* Using the default console logger (enabled automatically in v0.8.0+)
* Writing messages at different log levels
* Seeing library-generated logs

### Custom Logger with Color

```bash
dotnet run --framework net9.0 -- custom
```

This example demonstrates:

* Implementing a custom logger callback
* Adding timestamps and colored output
* Formatting log messages

### File Logging (via Custom Logger)

```bash
dotnet run --framework net9.0 -- file
```

This example shows:

* Implementing file logging using a custom logger callback
* Writing logs to a file (`/tmp/iceoryx2_csharp.log`)
* Viewing library logs in both console and file

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
dotnet run --framework net9.0 -- basic

# Set to Trace level (most verbose)
export IOX2_LOG_LEVEL=TRACE
dotnet run --framework net9.0 -- basic

# Set to Warn level (less verbose)
export IOX2_LOG_LEVEL=WARN
dotnet run --framework net9.0 -- basic
```

## API Usage

### Basic Logging (v0.8.0+)

```csharp
using Iceoryx2;

// Console logger is enabled by default - no initialization needed!

// Set log level
Iox2Log.SetLogLevel(LogLevel.Debug);

// Write log message
Iox2Log.Write(LogLevel.Info, "MyApp", "Application started");
```

### Environment-based Configuration

```csharp
// Set log level from IOX2_LOG_LEVEL environment variable, default to Info
Iox2Log.SetLogLevelFromEnvOrDefault();

// Or with custom default
Iox2Log.SetLogLevelFromEnvOr(LogLevel.Debug);
```

### Custom Logger

```csharp
// Set custom logger (can only be called once, before any log messages)
bool success = Iox2Log.SetLogger((level, origin, message) =>
{
    Console.WriteLine($"[{level}] {origin}: {message}");
});
```

### Custom File Logger

```csharp
// Implement file logging via custom logger callback
Iox2Log.SetLogger((level, origin, message) =>
{
    var logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
    File.AppendAllText("/tmp/myapp.log", logLine + Environment.NewLine);
});
Iox2Log.Write(LogLevel.Info, "MyApp", "This goes to the file");
```

## Notes

* The console logger is enabled by default in iceoryx2 v0.8.0+
* Custom loggers can only be set once and must be set before any log messages
  are created
* Library-generated logs (from iceoryx2 itself) will use the configured logger
* Origin can be null or empty if not needed
* For native file logging support, rebuild iceoryx2 with the appropriate
  feature flags
