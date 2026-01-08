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

using Iceoryx2.Native;
using System;
using System.Runtime.InteropServices;

namespace Iceoryx2;

/// <summary>
/// Provides logging functionality for iceoryx2.
/// Allows configuration of log levels, custom loggers, and built-in console/file logging.
/// </summary>
/// <remarks>
/// <para>
/// Starting with iceoryx2 v0.8.0, the console logger is enabled by default.
/// The file logger requires rebuilding iceoryx2 with specific feature flags.
/// </para>
/// <para>
/// Available loggers (configured via feature flags on iceoryx2-loggers crate):
/// <list type="bullet">
///   <item><description><b>console</b> - outputs to console (default)</description></item>
///   <item><description><b>buffer</b> - outputs to a buffer</description></item>
///   <item><description><b>file</b> - outputs to a file (requires rebuild with features)</description></item>
///   <item><description><b>log</b> - utilizes the log crate</description></item>
///   <item><description><b>tracing</b> - utilizes the tracing crate</description></item>
/// </list>
/// </para>
/// <para>
/// To enable the file logger, rebuild iceoryx2 with:
/// <code>
/// cargo build --package iceoryx2-ffi-c --features iceoryx2-loggers/std --features iceoryx2-loggers/file --no-default-features --release
/// </code>
/// </para>
/// <para>
/// Supported log levels: trace, debug, info, warning, error, fatal
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Set log level from environment variable IOX2_LOG_LEVEL, default to Info
/// Iox2Log.SetLogLevelFromEnvOrDefault();
///
/// // Or set specific log level
/// Iox2Log.SetLogLevel(LogLevel.Debug);
///
/// // Manual logging
/// Iox2Log.Write(LogLevel.Info, "MyApp", "Application started");
///
/// // Custom logger (set at runtime, must be done before any log messages)
/// Iox2Log.SetLogger((level, origin, message) =>
/// {
///     Console.WriteLine($"[{level}] {origin}: {message}");
/// });
/// </code>
/// </example>
public static class Iox2Log
{
    /// <summary>
    /// Delegate for custom log callbacks.
    /// </summary>
    /// <param name="logLevel">The severity level of the log message</param>
    /// <param name="origin">The source/origin of the log message</param>
    /// <param name="message">The log message content</param>
    public delegate void LogCallback(LogLevel logLevel, string origin, string message);

    private static Iox2NativeMethods.iox2_log_callback? _nativeCallback;

    /// <summary>
    /// Writes a log message to the logger.
    /// </summary>
    /// <param name="logLevel">The severity level of the message</param>
    /// <param name="origin">The source/origin of the message (can be null)</param>
    /// <param name="message">The log message content</param>
    public static void Write(LogLevel logLevel, string? origin, string message)
    {
        unsafe
        {
            fixed (byte* originPtr = origin != null ? System.Text.Encoding.UTF8.GetBytes(origin + "\0") : null)
            fixed (byte* messagePtr = System.Text.Encoding.UTF8.GetBytes(message + "\0"))
            {
                Iox2NativeMethods.iox2_log(
                    (Iox2NativeMethods.iox2_log_level_e)logLevel,
                    (IntPtr)originPtr,
                    (IntPtr)messagePtr
                );
            }
        }
    }

    /// <summary>
    /// Sets the console logger as the default logger.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Starting with iceoryx2 v0.8.0, the console logger is enabled by default.
    /// This method is now a no-op and always returns true for backward compatibility.
    /// </para>
    /// <para>
    /// To use a different logger backend, you can either:
    /// <list type="bullet">
    ///   <item><description>Use <see cref="SetLogger"/> to set a custom logger callback at runtime</description></item>
    ///   <item><description>Rebuild iceoryx2 with different feature flags (e.g., file, log, tracing)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <returns>Always returns true as the console logger is enabled by default</returns>
    [Obsolete("Console logger is now enabled by default in iceoryx2 v0.8.0+. This method is a no-op.")]
    public static bool UseConsoleLogger()
    {
        // Console logger is now enabled by default in iceoryx2 v0.8.0+
        // This method is kept for backward compatibility
        return true;
    }

    /// <summary>
    /// Sets the file logger as the default logger.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Starting with iceoryx2 v0.8.0, the file logger requires rebuilding the iceoryx2 native library
    /// with specific feature flags. This method will throw <see cref="NotSupportedException"/> if the
    /// native library was not built with file logger support.
    /// </para>
    /// <para>
    /// To enable file logging, rebuild iceoryx2 with:
    /// <code>
    /// cargo build --package iceoryx2-ffi-c --features iceoryx2-loggers/std --features iceoryx2-loggers/file --no-default-features --release
    /// </code>
    /// </para>
    /// <para>
    /// Alternative: Use <see cref="SetLogger"/> to implement custom file logging at runtime.
    /// </para>
    /// </remarks>
    /// <param name="logFile">Path to the log file</param>
    /// <returns>True if the logger was set successfully, false otherwise</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when the native library was not built with file logger support.
    /// </exception>
    [Obsolete("File logger requires rebuilding iceoryx2 with --features iceoryx2-loggers/file. Consider using SetLogger() for custom file logging.")]
    public static bool UseFileLogger(string logFile)
    {
        // File logger requires rebuilding iceoryx2 with specific feature flags in v0.8.0+
        // The native function iox2_use_file_logger has been removed from default builds
        throw new NotSupportedException(
            "File logger is not available in the default iceoryx2 build. " +
            "To enable file logging, either:\n" +
            "1. Rebuild iceoryx2 with: cargo build --package iceoryx2-ffi-c --features iceoryx2-loggers/std --features iceoryx2-loggers/file --no-default-features --release\n" +
            "2. Use Iox2Log.SetLogger() to implement custom file logging at runtime.");
    }

    /// <summary>
    /// Sets the log level from the IOX2_LOG_LEVEL environment variable,
    /// or defaults to Info if the variable is not set.
    /// </summary>
    public static void SetLogLevelFromEnvOrDefault()
    {
        Iox2NativeMethods.iox2_set_log_level_from_env_or_default();
    }

    /// <summary>
    /// Sets the log level from the IOX2_LOG_LEVEL environment variable,
    /// or uses the specified level if the variable is not set.
    /// </summary>
    /// <param name="level">The fallback log level to use if the environment variable is not set</param>
    public static void SetLogLevelFromEnvOr(LogLevel level)
    {
        Iox2NativeMethods.iox2_set_log_level_from_env_or((Iox2NativeMethods.iox2_log_level_e)level);
    }

    /// <summary>
    /// Sets the current log level.
    /// This is ignored for external logging frameworks.
    /// </summary>
    /// <param name="level">The log level to set</param>
    public static void SetLogLevel(LogLevel level)
    {
        Iox2NativeMethods.iox2_set_log_level((Iox2NativeMethods.iox2_log_level_e)level);
    }

    /// <summary>
    /// Gets the current log level.
    /// </summary>
    /// <returns>The current log level</returns>
    public static LogLevel GetLogLevel()
    {
        return (LogLevel)Iox2NativeMethods.iox2_get_log_level();
    }

    /// <summary>
    /// Sets a custom logger callback.
    /// This function can only be called once and must be called before any log message is created.
    /// </summary>
    /// <param name="callback">The custom log callback function</param>
    /// <returns>True if the logger was set successfully, false otherwise</returns>
    public static bool SetLogger(LogCallback callback)
    {
        // Keep a reference to prevent garbage collection
        _nativeCallback = (level, origin, message) =>
        {
            unsafe
            {
                var originStr = origin != IntPtr.Zero
                    ? Marshal.PtrToStringUTF8(origin) ?? string.Empty
                    : string.Empty;
                var messageStr = Marshal.PtrToStringUTF8(message) ?? string.Empty;
                callback((LogLevel)level, originStr, messageStr);
            }
        };

        return Iox2NativeMethods.iox2_set_logger(_nativeCallback);
    }
}