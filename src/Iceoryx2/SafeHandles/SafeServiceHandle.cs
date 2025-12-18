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
/// Safe handle for Service resources (Port Factory).
/// </summary>
internal sealed class SafeServiceHandle : SafeIox2Handle
{
    public SafeServiceHandle() : base()
    {
    }

    public SafeServiceHandle(IntPtr handle) : base(handle)
    {
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid && handle != IntPtr.Zero)
        {
            Native.Iox2NativeMethods.iox2_port_factory_pub_sub_drop(handle);
            return true;
        }
        return false;
    }
}