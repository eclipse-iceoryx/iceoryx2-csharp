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
/// Controls whether the WaitSet callback should continue processing more events or stop.
/// </summary>
public enum CallbackProgression
{
    /// <summary>
    /// Stop processing events and return from WaitSet.WaitAndProcess().
    /// </summary>
    Stop = 0,

    /// <summary>
    /// Continue processing the next event if available.
    /// </summary>
    Continue = 1
}