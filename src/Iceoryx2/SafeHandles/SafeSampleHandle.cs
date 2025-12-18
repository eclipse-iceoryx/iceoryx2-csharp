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

namespace Iceoryx2.SafeHandles;

/// <summary>
/// Safe handle for Sample resources.
/// </summary>
internal sealed class SafeSampleHandle : SafeIox2Handle
{
    private readonly bool _isMutable;

    public bool IsMutable => _isMutable;

    public SafeSampleHandle(bool isMutable = false) : base()
    {
        _isMutable = isMutable;
    }

    public SafeSampleHandle(IntPtr handle, bool isMutable = false) : base(handle)
    {
        _isMutable = isMutable;
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid && handle != IntPtr.Zero)
        {
            if (_isMutable)
                Native.Iox2NativeMethods.iox2_sample_mut_drop(handle);
            else
                Native.Iox2NativeMethods.iox2_sample_drop(handle);
            return true;
        }
        return false;
    }
}