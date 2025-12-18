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
/// Defines how the WaitSet handles POSIX signals (SIGTERM, SIGINT).
/// </summary>
public enum SignalHandlingMode
{
    /// <summary>
    /// Signal handling is disabled. The WaitSet will not wake up on signals.
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// Wake up on SIGTERM (termination signal).
    /// </summary>
    Termination = 1,

    /// <summary>
    /// Wake up on SIGINT (interrupt signal, e.g., Ctrl+C).
    /// </summary>
    Interrupt = 2,

    /// <summary>
    /// Wake up on both SIGTERM and SIGINT.
    /// </summary>
    TerminationAndInterrupt = 3
}