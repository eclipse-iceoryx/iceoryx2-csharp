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

#if NET6_0_OR_GREATER

using Microsoft.Extensions.Logging;
using System;

namespace Iceoryx2.Extensions;

/// <summary>
/// Integrates iceoryx2 native logging with Microsoft.Extensions.Logging infrastructure.
/// This allows you to see internal iceoryx2 logs through your existing logging setup
/// (Serilog, NLog, Console, etc.).
/// </summary>
/// <example>
/// <code>
/// // In your application startup (e.g., Program.cs):
/// var builder = WebApplication.CreateBuilder(args);
/// 
/// // Configure your logging
/// builder.Logging.AddConsole();
/// builder.Logging.AddDebug();
/// 
/// // Integrate iceoryx2 logging
/// builder.Services.AddIceoryx2Logging(options =>
/// {
///     options.LogLevel = Iceoryx2.LogLevel.Debug;
///     options.CategoryName = "Iceoryx2";
/// });
/// 
/// // Or with ILoggerFactory:
/// var loggerFactory = LoggerFactory.Create(builder =>
/// {
///     builder.AddConsole();
/// });
/// 
/// Iox2LoggingExtensions.UseExtensionsLogging(loggerFactory, options =>
/// {
///     options.LogLevel = Iceoryx2.LogLevel.Debug;
/// });
/// </code>
/// </example>
public static class Iox2LoggingExtensions
{
    private static ILoggerFactory? _loggerFactory;
    private static string _categoryName = "Iceoryx2";

    /// <summary>
    /// Configures iceoryx2 to use Microsoft.Extensions.Logging infrastructure.
    /// </summary>
    /// <param name="loggerFactory">The logger factory to use for creating loggers</param>
    /// <param name="configure">Optional configuration action</param>
    /// <returns>True if logging was successfully configured, false otherwise</returns>
    public static bool UseExtensionsLogging(
        ILoggerFactory loggerFactory,
        Action<Iox2LoggingOptions>? configure = null)
    {
        var options = new Iox2LoggingOptions();
        configure?.Invoke(options);

        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _categoryName = options.CategoryName;

        var logger = _loggerFactory.CreateLogger(_categoryName);

        // Set the iceoryx2 native log level
        Iox2Log.SetLogLevel(options.LogLevel);

        // Register custom logger callback
        var success = Iox2Log.SetLogger((level, origin, message) =>
        {
            var msLogLevel = MapLogLevel(level);

            // Use structured logging with origin as a scope
            if (!string.IsNullOrEmpty(origin))
            {
                using (logger.BeginScope(new { Origin = origin }))
                {
                    logger.Log(msLogLevel, "[{Origin}] {Message}", origin, message);
                }
            }
            else
            {
                logger.Log(msLogLevel, "{Message}", message);
            }
        });

        if (!success)
        {
            logger.LogWarning(
                "Failed to set iceoryx2 logger. Logger can only be set once and must be called before any log messages are created.");
        }

        return success;
    }

    /// <summary>
    /// Maps iceoryx2 LogLevel to Microsoft.Extensions.Logging LogLevel.
    /// </summary>
    private static Microsoft.Extensions.Logging.LogLevel MapLogLevel(Iceoryx2.LogLevel level)
    {
        return level switch
        {
            Iceoryx2.LogLevel.Trace => Microsoft.Extensions.Logging.LogLevel.Trace,
            Iceoryx2.LogLevel.Debug => Microsoft.Extensions.Logging.LogLevel.Debug,
            Iceoryx2.LogLevel.Info => Microsoft.Extensions.Logging.LogLevel.Information,
            Iceoryx2.LogLevel.Warn => Microsoft.Extensions.Logging.LogLevel.Warning,
            Iceoryx2.LogLevel.Error => Microsoft.Extensions.Logging.LogLevel.Error,
            Iceoryx2.LogLevel.Fatal => Microsoft.Extensions.Logging.LogLevel.Critical,
            _ => Microsoft.Extensions.Logging.LogLevel.Information
        };
    }
}

/// <summary>
/// Configuration options for iceoryx2 logging integration.
/// </summary>
public class Iox2LoggingOptions
{
    /// <summary>
    /// Gets or sets the iceoryx2 native log level.
    /// Default is Info.
    /// </summary>
    public Iceoryx2.LogLevel LogLevel { get; set; } = Iceoryx2.LogLevel.Info;

    /// <summary>
    /// Gets or sets the category name for the logger.
    /// This is the name that will appear in log outputs.
    /// Default is "Iceoryx2".
    /// </summary>
    public string CategoryName { get; set; } = "Iceoryx2";
}

#endif