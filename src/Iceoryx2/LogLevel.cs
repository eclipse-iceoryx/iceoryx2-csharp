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

namespace Iceoryx2;

/// <summary>
/// Represents the log level for iceoryx2 logging.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Trace level - most verbose logging for detailed debugging
    /// </summary>
    Trace = 0,

    /// <summary>
    /// Debug level - debug information for development
    /// </summary>
    Debug = 1,

    /// <summary>
    /// Info level - informational messages about normal operation
    /// </summary>
    Info = 2,

    /// <summary>
    /// Warn level - warning messages for potentially harmful situations
    /// </summary>
    Warn = 3,

    /// <summary>
    /// Error level - error messages for failures that don't terminate the application
    /// </summary>
    Error = 4,

    /// <summary>
    /// Fatal level - critical errors that may cause termination
    /// </summary>
    Fatal = 5
}