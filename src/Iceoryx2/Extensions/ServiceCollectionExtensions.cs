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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace Iceoryx2.Extensions;

/// <summary>
/// Extension methods for configuring iceoryx2 logging with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds iceoryx2 logging integration to the service collection.
    /// This configures iceoryx2 to route its internal logs through Microsoft.Extensions.Logging.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Optional configuration action</param>
    /// <returns>The service collection for chaining</returns>
    /// <example>
    /// <code>
    /// // In Program.cs or Startup.cs:
    /// builder.Services.AddIceoryx2Logging(options =>
    /// {
    ///     options.LogLevel = Iceoryx2.LogLevel.Debug;
    ///     options.CategoryName = "MyApp.Iceoryx2";
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddIceoryx2Logging(
        this IServiceCollection services,
        Action<Iox2LoggingOptions>? configure = null)
    {
        var options = new Iox2LoggingOptions();
        configure?.Invoke(options);

        // Register as a hosted service to initialize logging on startup
        services.AddSingleton<IIox2LoggingInitializer>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return new Iox2LoggingInitializer(loggerFactory, options);
        });

        // Initialize immediately if we have a service provider
        // Otherwise it will be initialized on first service resolution
        return services;
    }

    /// <summary>
    /// Adds iceoryx2 logging integration and initializes it immediately.
    /// Use this when you have access to a built service provider.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="serviceProvider">The built service provider</param>
    /// <param name="configure">Optional configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAndInitializeIceoryx2Logging(
        this IServiceCollection services,
        IServiceProvider serviceProvider,
        Action<Iox2LoggingOptions>? configure = null)
    {
        services.AddIceoryx2Logging(configure);

        // Initialize immediately
        var initializer = serviceProvider.GetRequiredService<IIox2LoggingInitializer>();
        initializer.Initialize();

        return services;
    }
}

/// <summary>
/// Interface for iceoryx2 logging initializer.
/// </summary>
public interface IIox2LoggingInitializer
{
    /// <summary>
    /// Initializes iceoryx2 logging.
    /// </summary>
    /// <returns>True if initialization was successful, false otherwise</returns>
    bool Initialize();

    /// <summary>
    /// Gets whether the logging has been initialized.
    /// </summary>
    bool IsInitialized { get; }
}

/// <summary>
/// Internal implementation of iceoryx2 logging initializer.
/// </summary>
internal class Iox2LoggingInitializer : IIox2LoggingInitializer
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly Iox2LoggingOptions _options;
    private bool _initialized;

    public Iox2LoggingInitializer(ILoggerFactory loggerFactory, Iox2LoggingOptions options)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public bool IsInitialized => _initialized;

    public bool Initialize()
    {
        if (_initialized)
        {
            return true;
        }

        var success = Iox2LoggingExtensions.UseExtensionsLogging(_loggerFactory, opts =>
        {
            opts.LogLevel = _options.LogLevel;
            opts.CategoryName = _options.CategoryName;
        });

        _initialized = success;
        return success;
    }
}

#endif