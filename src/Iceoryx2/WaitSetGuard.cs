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

using Iceoryx2.SafeHandles;
using System;

namespace Iceoryx2;

/// <summary>
/// RAII guard that maintains an attachment to a WaitSet.
/// When disposed, the attachment is automatically removed from the WaitSet.
/// Keep the guard alive for as long as you want the attachment to remain active.
/// </summary>
public sealed class WaitSetGuard : IDisposable
{
    private SafeWaitSetGuardHandle _handle;
    private bool _disposed;

    internal WaitSetGuard(SafeWaitSetGuardHandle handle)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
    }

    /// <summary>
    /// Gets the internal handle (for internal use by WaitSetAttachmentId).
    /// </summary>
    internal IntPtr GetHandle()
    {
        ThrowIfDisposed();
        return _handle.DangerousGetHandle();
    }

    /// <summary>
    /// Disposes the guard and detaches from the WaitSet.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _handle?.Dispose();
            _disposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WaitSetGuard));
    }
}