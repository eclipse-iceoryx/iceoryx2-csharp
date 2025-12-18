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
using System;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0 || args[0] == "basic")
        {
            RunBasicLoggingExample();
        }
        else if (args[0] == "custom")
        {
            RunCustomLoggerExample();
        }
        else if (args[0] == "file")
        {
            RunFileLoggerExample();
        }
        else
        {
            Console.WriteLine("Usage: dotnet run [basic|custom|file]");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  dotnet run basic   - Use console logger with environment variable");
            Console.WriteLine("  dotnet run custom  - Use custom logger callback");
            Console.WriteLine("  dotnet run file    - Use file logger");
        }
    }

    static void RunBasicLoggingExample()
    {
        Console.WriteLine("=== Basic Logging Example ===");
        Console.WriteLine();

        // Set log level from environment variable IOX2_LOG_LEVEL (or default to Info)
        Iox2Log.SetLogLevelFromEnvOrDefault();

        // Use the built-in console logger
        if (Iox2Log.UseConsoleLogger())
        {
            Console.WriteLine("Console logger initialized successfully");
        }

        Console.WriteLine($"Current log level: {Iox2Log.GetLogLevel()}");
        Console.WriteLine();

        // Iox2Log messages at different levels
        Iox2Log.Write(LogLevel.Trace, "ExampleApp", "This is a TRACE message (usually not visible)");
        Iox2Log.Write(LogLevel.Debug, "ExampleApp", "This is a DEBUG message");
        Iox2Log.Write(LogLevel.Info, "ExampleApp", "This is an INFO message");
        Iox2Log.Write(LogLevel.Warn, "ExampleApp", "This is a WARN message");
        Iox2Log.Write(LogLevel.Error, "ExampleApp", "This is an ERROR message");

        Console.WriteLine();
        Console.WriteLine("Try setting IOX2_LOG_LEVEL environment variable:");
        Console.WriteLine("  export IOX2_LOG_LEVEL=TRACE");
        Console.WriteLine("  export IOX2_LOG_LEVEL=DEBUG");
        Console.WriteLine("  export IOX2_LOG_LEVEL=WARN");

        // Create a simple service to see library logs
        Console.WriteLine();
        Console.WriteLine("Creating iceoryx2 node (will generate library logs)...");
        var node = NodeBuilder.New()
            .Name("logging_example")
            .Create()
            .Expect("Failed to create node");

        Console.WriteLine("Node created successfully!");
    }

    static void RunCustomLoggerExample()
    {
        Console.WriteLine("=== Custom Logger Example ===");
        Console.WriteLine();

        // Set custom logger callback
        bool success = Iox2Log.SetLogger((level, origin, message) =>
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var levelStr = level.ToString().ToUpper().PadRight(5);
            var color = level switch
            {
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Fatal => ConsoleColor.DarkRed,
                LogLevel.Warn => ConsoleColor.Yellow,
                LogLevel.Info => ConsoleColor.Green,
                LogLevel.Debug => ConsoleColor.Cyan,
                LogLevel.Trace => ConsoleColor.Gray,
                _ => ConsoleColor.White
            };

            Console.ForegroundColor = color;
            if (string.IsNullOrEmpty(origin))
            {
                Console.WriteLine($"[{timestamp}] [{levelStr}] {message}");
            }
            else
            {
                Console.WriteLine($"[{timestamp}] [{levelStr}] {origin} - {message}");
            }
            Console.ResetColor();
        });

        if (success)
        {
            Console.WriteLine("Custom logger set successfully!");
        }
        else
        {
            Console.WriteLine("Failed to set custom logger (may have been set already)");
            return;
        }

        Console.WriteLine();

        // Set log level to see all messages
        Iox2Log.SetLogLevel(LogLevel.Trace);

        // Iox2Log messages at different levels
        Iox2Log.Write(LogLevel.Trace, "CustomApp", "Trace: Very detailed debugging information");
        Iox2Log.Write(LogLevel.Debug, "CustomApp", "Debug: Debugging information");
        Iox2Log.Write(LogLevel.Info, "CustomApp", "Info: General information");
        Iox2Log.Write(LogLevel.Warn, "CustomApp", "Warn: Warning message");
        Iox2Log.Write(LogLevel.Error, "CustomApp", "Error: Something went wrong");
        Iox2Log.Write(LogLevel.Fatal, "CustomApp", "Fatal: Critical error!");

        Console.WriteLine();

        // Create a node to see library logs with custom formatting
        Console.WriteLine("Creating iceoryx2 node with custom logger...");
        var node = NodeBuilder.New()
            .Name("custom_logging_example")
            .Create()
            .Expect("Failed to create node");

        Console.WriteLine("Node created!");
    }

    static void RunFileLoggerExample()
    {
        Console.WriteLine("=== File Logger Example ===");
        Console.WriteLine();

        var logFile = "/tmp/iceoryx2_csharp.log";

        // Use file logger
        if (Iox2Log.UseFileLogger(logFile))
        {
            Console.WriteLine($"File logger initialized: {logFile}");
        }
        else
        {
            Console.WriteLine("Failed to initialize file logger");
            return;
        }

        // Set log level
        Iox2Log.SetLogLevel(LogLevel.Debug);
        Console.WriteLine($"Iox2Log level set to: {Iox2Log.GetLogLevel()}");
        Console.WriteLine();

        // Write some log messages
        Console.WriteLine("Writing log messages to file...");
        Iox2Log.Write(LogLevel.Debug, "FileLogging", "Debug message to file");
        Iox2Log.Write(LogLevel.Info, "FileLogging", "Info message to file");
        Iox2Log.Write(LogLevel.Warn, "FileLogging", "Warning message to file");
        Iox2Log.Write(LogLevel.Error, "FileLogging", "Error message to file");

        // Create a node to generate library logs
        Console.WriteLine("Creating iceoryx2 node (logs will be written to file)...");
        var node = NodeBuilder.New()
            .Name("file_logging_example")
            .Create()
            .Expect("Failed to create node");

        Console.WriteLine("Node created!");
        Console.WriteLine();
        Console.WriteLine($"Check the log file: cat {logFile}");
    }
}