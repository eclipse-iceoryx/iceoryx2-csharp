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
/// The result of WaitSet.WaitAndProcess() or WaitSet.WaitAndProcessOnce().
/// Indicates why the wait operation completed.
/// </summary>
public enum WaitSetRunResult
{
    /// <summary>
    /// A SIGTERM signal was received (termination request).
    /// </summary>
    TerminationRequest = 1,

    /// <summary>
    /// A SIGINT signal was received (interrupt, e.g., Ctrl+C).
    /// </summary>
    Interrupt = 2,

    /// <summary>
    /// User callback returned CallbackProgression.Stop.
    /// </summary>
    StopRequest = 3,

    /// <summary>
    /// All pending events have been processed.
    /// </summary>
    AllEventsHandled = 4
}