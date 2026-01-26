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
/// Safe handle for blackboard reader resources.
/// Ensures proper cleanup of native resources when disposed.
/// </summary>
internal sealed class SafeReaderHandle : SafeIox2Handle
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SafeReaderHandle"/> class.
    /// </summary>
    public SafeReaderHandle() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified handle.
    /// </summary>
    public SafeReaderHandle(IntPtr handle) : base(handle)
    {
    }

    /// <summary>
    /// Releases the native reader handle.
    /// </summary>
    /// <returns>true if the handle was released successfully; otherwise, false.</returns>
    protected override bool ReleaseHandle()
    {
        if (!IsInvalid)
        {
            iox2_reader_drop(handle);
        }
        return true;
    }
}