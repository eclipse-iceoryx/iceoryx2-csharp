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
/// Allows configuration of log levels, custom loggers, and built-in file/console logging.
/// </summary>
/// <example>
/// <code>
/// // Set log level from environment variable IOX2_LOG_LEVEL, default to Info
/// Iox2Log.SetLogLevelFromEnvOrDefault();
/// 
/// // Or set specific log level
/// Iox2Log.SetLogLevel(LogLevel.Debug);
/// 
/// // Use console logger
/// Iox2Log.UseConsoleLogger();
/// 
/// // Or use file logger
/// Iox2Log.UseFileLogger("/tmp/iceoryx2.log");
/// 
/// // Manual logging
/// Iox2Log.Write(LogLevel.Info, "MyApp", "Application started");
/// 
/// // Custom logger
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
    /// <returns>True if the logger was set successfully, false otherwise</returns>
    public static bool UseConsoleLogger()
    {
        return Iox2NativeMethods.iox2_use_console_logger();
    }

    /// <summary>
    /// Sets the file logger as the default logger.
    /// </summary>
    /// <param name="logFile">Path to the log file</param>
    /// <returns>True if the logger was set successfully, false otherwise</returns>
    public static bool UseFileLogger(string logFile)
    {
        unsafe
        {
            fixed (byte* logFilePtr = System.Text.Encoding.UTF8.GetBytes(logFile + "\0"))
            {
                return Iox2NativeMethods.iox2_use_file_logger((IntPtr)logFilePtr);
            }
        }
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