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

using Microsoft.Win32.SafeHandles;
using System;

namespace Iceoryx2.SafeHandles;

/// <summary>
/// Safe handle for native iceoryx2 resources.
/// Ensures proper cleanup of native resources even if Dispose is not called.
/// </summary>
internal abstract class SafeIox2Handle : SafeHandleZeroOrMinusOneIsInvalid
{
    protected SafeIox2Handle() : base(true)
    {
    }

    protected SafeIox2Handle(IntPtr handle) : base(true)
    {
        SetHandle(handle);
    }
}