// Copyright (c) 2025 Contributors to the Eclipse Foundation
//
// See the NOTICE file(s) distributed with this work for additional
// information regarding copyright ownership.
//
// This program and the accompanying materials are made available under the
// terms of the Apache Software License 2.0 which is available at
// https://www.apache.org/licenses/LICENSE-2.0, or the MIT license
// which is available at https://opensource.org/licenses/MIT.
//
// SPDX-License-Identifier: Apache-2.0 OR MIT

using Iceoryx2;
using Iceoryx2.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Threading.Tasks;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Iox2LogLevel = Iceoryx2.LogLevel;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

/// <summary>
/// Demonstrates iceoryx2 integration with Microsoft.Extensions.Logging.
/// Shows how to see internal iceoryx2 logs through popular logging frameworks:
/// - Microsoft.Extensions.Logging (Console)
/// - Serilog
/// - Custom loggers
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Iceoryx2 Logging Integration Examples");
            Console.WriteLine();
            Console.WriteLine("Demonstrates how to integrate iceoryx2 with Microsoft.Extensions.Logging");
            Console.WriteLine("and popular logging frameworks like Serilog, NLog, etc.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run --framework net9.0 -- console");
            Console.WriteLine("  dotnet run --framework net9.0 -- serilog");
            Console.WriteLine("  dotnet run --framework net9.0 -- di");
            Console.WriteLine("  dotnet run --framework net9.0 -- custom");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  console  - Use Microsoft.Extensions.Logging console provider");
            Console.WriteLine("  serilog  - Use Serilog for structured logging");
            Console.WriteLine("  di       - Use Dependency Injection setup (ASP.NET Core style)");
            Console.WriteLine("  custom   - Use a custom logger callback");
            return -1;
        }

        var example = args[0].ToLower();

        return example switch
        {
            "console" => await RunConsoleLoggingExample(),
            "serilog" => await RunSerilogExample(),
            "di" => await RunDependencyInjectionExample(),
            "custom" => await RunCustomLoggerExample(),
            _ => ShowUsage()
        };
    }

    static int ShowUsage()
    {
        Console.WriteLine("Unknown example. Use 'console', 'serilog', 'di', or 'custom'");
        return -1;
    }

    /// <summary>
    /// Example 1: Using Microsoft.Extensions.Logging with Console provider
    /// </summary>
    static async Task<int> RunConsoleLoggingExample()
    {
        // Create a logger factory with console logging
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(MsLogLevel.Trace)
                .AddConsole();
        });

        var logger = loggerFactory.CreateLogger("Example");

        logger.LogInformation("═══ Example 1: Microsoft.Extensions.Logging (Console) ═══");
        logger.LogInformation("");

        // Integrate iceoryx2 logging
        var success = Iox2LoggingExtensions.UseExtensionsLogging(loggerFactory, options =>
        {
            options.LogLevel = Iox2LogLevel.Debug;
            options.CategoryName = "Iceoryx2";
        });

        if (!success)
        {
            logger.LogError("Failed to setup logging!");
            return -1;
        }

        logger.LogInformation("Logging configured - iceoryx2 logs will appear below");
        logger.LogInformation("");

        // Now use iceoryx2 - internal logs will appear through our console logger
        await RunSimplePublisher("console_logging_demo", loggerFactory.CreateLogger("Publisher"));

        return 0;
    }

    /// <summary>
    /// Example 2: Using Serilog for structured logging
    /// </summary>
    static async Task<int> RunSerilogExample()
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        var logger = Log.Logger.ForContext<Program>();

        logger.Information("═══ Example 2: Serilog (Structured Logging) ═══");
        logger.Information("");

        // Create logger factory with Serilog
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSerilog(Log.Logger);
        });

        // Integrate iceoryx2 logging
        var success = Iox2LoggingExtensions.UseExtensionsLogging(loggerFactory, options =>
        {
            options.LogLevel = Iox2LogLevel.Debug;
            options.CategoryName = "Iceoryx2";
        });

        if (!success)
        {
            logger.Error("Failed to setup logging!");
            return -1;
        }

        logger.Information("Serilog configured - iceoryx2 logs will appear with structured data");
        logger.Information("");

        // Use iceoryx2
        await RunSimplePublisher("serilog_demo", loggerFactory.CreateLogger("Publisher"));

        Log.CloseAndFlush();
        return 0;
    }

    /// <summary>
    /// Example 3: Using Dependency Injection (ASP.NET Core style)
    /// </summary>
    static async Task<int> RunDependencyInjectionExample()
    {
        // Setup DI container (like in ASP.NET Core)
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder =>
        {
            builder
                .SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug)
                .AddConsole();
        });

        // Add iceoryx2 logging integration
        services.AddIceoryx2Logging(options =>
        {
            options.LogLevel = Iox2LogLevel.Debug;
            options.CategoryName = "MyApp.Iceoryx2";
        });

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Example");

        logger.LogInformation("═══ Example 3: Dependency Injection (ASP.NET Core style) ═══");
        logger.LogInformation("");

        // Initialize iceoryx2 logging
        var initializer = serviceProvider.GetRequiredService<IIox2LoggingInitializer>();
        if (!initializer.Initialize())
        {
            logger.LogError("Failed to initialize logging!");
            return -1;
        }

        logger.LogInformation("DI logging configured");
        logger.LogInformation("");

        // Use iceoryx2
        await RunSimplePublisher("di_demo", serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Publisher"));

        return 0;
    }

    /// <summary>
    /// Example 4: Custom logger callback
    /// Note: This example uses Console.WriteLine for demonstration purposes
    /// to show the custom logger output without interference from M.E.Logging
    /// </summary>
    static async Task<int> RunCustomLoggerExample()
    {
        Console.WriteLine("═══ Example 4: Custom Logger Callback ═══");
        Console.WriteLine();

        // Set custom logger with color-coded output
        var success = Iox2Log.SetLogger((level, origin, message) =>
        {
            var originalColor = Console.ForegroundColor;

            // Color code by level
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

            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var levelStr = level.ToString().ToUpper().PadRight(5);
            var originStr = !string.IsNullOrEmpty(origin) ? $"[{origin}]" : "";

            Console.WriteLine($"[{timestamp}] {levelStr} {originStr} {message}");

            Console.ForegroundColor = originalColor;
        });

        if (!success)
        {
            Console.WriteLine("Failed to setup custom logger!");
            return -1;
        }

        // Set log level
        Iox2Log.SetLogLevel(Iox2LogLevel.Debug);

        Console.WriteLine("Custom logger configured with color-coded output");
        Console.WriteLine();

        // Create a simple logger for the publisher demo (won't interfere with custom iceoryx2 logs)
        using var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(MsLogLevel.Information).AddConsole());
        await RunSimplePublisher("custom_logger_demo", loggerFactory.CreateLogger("Publisher"));

        return 0;
    }

    /// <summary>
    /// Helper: Runs a simple publisher to generate some iceoryx2 activity
    /// This will trigger internal iceoryx2 logs that you'll see through your logger
    /// </summary>
    static async Task RunSimplePublisher(string serviceName, ILogger logger)
    {
        logger.LogInformation("Creating node and service '{ServiceName}'...", serviceName);
        logger.LogInformation("");

        var node = NodeBuilder.New()
            .Create()
            .Expect("Failed to create node");

        var service = node.ServiceBuilder()
            .PublishSubscribe<ulong>()
            .Open(serviceName)
            .Expect($"Failed to open service '{serviceName}'");

        var publisher = service.CreatePublisher()
            .Expect("Failed to create publisher");

        logger.LogInformation("Publishing 5 samples...");
        logger.LogInformation("");

        for (ulong i = 0; i < 5; i++)
        {
            publisher.SendCopy(i).Expect("Failed to send sample");
            logger.LogInformation("  Published: {Counter}", i);
            await Task.Delay(200);
        }

        logger.LogInformation("");
        logger.LogInformation("Cleaning up...");
        logger.LogInformation("");

        publisher.Dispose();
        service.Dispose();
        node.Dispose();

        logger.LogInformation("Example completed!");
        logger.LogInformation("");
    }
}