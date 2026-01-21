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

using System;
using static Iceoryx2.Native.Iox2NativeMethods;

namespace Iceoryx2.SafeHandles;

/// <summary>
/// Safe handle for blackboard entry value (uninit) resources.
/// Ensures proper cleanup of native resources when disposed.
/// </summary>
internal sealed class SafeEntryValueHandle : SafeIox2Handle
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SafeEntryValueHandle"/> class.
    /// </summary>
    public SafeEntryValueHandle() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified handle.
    /// </summary>
    public SafeEntryValueHandle(IntPtr handle) : base(handle)
    {
    }

    /// <summary>
    /// Consumes the handle, returning the raw pointer and marking the handle as invalid.
    /// This is used when ownership of the handle is transferred to another object (e.g., during Update()).
    /// After calling this method, the SafeHandle will not release the native resource.
    /// </summary>
    /// <returns>The raw handle pointer.</returns>
    public IntPtr Consume()
    {
        var rawHandle = handle;
        SetHandleAsInvalid();
        return rawHandle;
    }

    /// <summary>
    /// Releases the native entry value handle.
    /// </summary>
    /// <returns>true if the handle was released successfully; otherwise, false.</returns>
    protected override bool ReleaseHandle()
    {
        if (!IsInvalid)
        {
            iox2_entry_value_drop(handle);
        }
        return true;
    }
}