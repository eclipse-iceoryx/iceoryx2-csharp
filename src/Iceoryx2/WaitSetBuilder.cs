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
/// Builder for creating a WaitSet.
/// </summary>
public sealed class WaitSetBuilder : IDisposable
{
    private SafeWaitSetBuilderHandle _handle;
    private bool _disposed;

    private WaitSetBuilder(SafeWaitSetBuilderHandle handle)
    {
        _handle = handle;
    }

    /// <summary>
    /// Creates a new WaitSetBuilder.
    /// </summary>
    /// <returns>A new WaitSetBuilder instance.</returns>
    public static WaitSetBuilder New()
    {
        Native.Iox2NativeMethods.iox2_waitset_builder_new(IntPtr.Zero, out var handle);
        if (handle == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create WaitSetBuilder");

        return new WaitSetBuilder(new SafeWaitSetBuilderHandle(handle));
    }

    /// <summary>
    /// Sets the signal handling mode for the WaitSet.
    /// Determines which POSIX signals will wake up the WaitSet.
    /// </summary>
    /// <param name="mode">The signal handling mode.</param>
    /// <returns>This builder for method chaining.</returns>
    public WaitSetBuilder SignalHandling(SignalHandlingMode mode)
    {
        ThrowIfDisposed();

        var handlePtr = _handle.DangerousGetHandle();
        Native.Iox2NativeMethods.iox2_waitset_builder_set_signal_handling_mode(
            ref handlePtr,
            (Native.Iox2NativeMethods.iox2_signal_handling_mode_e)mode);

        return this;
    }

    /// <summary>
    /// Creates the WaitSet for IPC services (default).
    /// </summary>
    /// <returns>Result containing the WaitSet on success, or an error.</returns>
    public Result<WaitSet, Iox2Error> Create()
    {
        return CreateInternal(Native.Iox2NativeMethods.iox2_service_type_e.IPC);
    }

    /// <summary>
    /// Creates the WaitSet for Local services.
    /// </summary>
    /// <returns>Result containing the WaitSet on success, or an error.</returns>
    public Result<WaitSet, Iox2Error> CreateLocal()
    {
        return CreateInternal(Native.Iox2NativeMethods.iox2_service_type_e.LOCAL);
    }

    private Result<WaitSet, Iox2Error> CreateInternal(Native.Iox2NativeMethods.iox2_service_type_e serviceType)
    {
        ThrowIfDisposed();

        var handlePtr = _handle.DangerousGetHandle();
        var result = Native.Iox2NativeMethods.iox2_waitset_builder_create(
            handlePtr,
            serviceType,
            IntPtr.Zero,
            out var waitsetHandle);

        // Builder is consumed on create (success or failure)
        _disposed = true;
        _handle.SetHandleAsInvalid();

        if (result != Native.Iox2NativeMethods.IOX2_OK)
        {
            return Result<WaitSet, Iox2Error>.Err(Iox2Error.WaitSetCreationFailed);
        }

        return Result<WaitSet, Iox2Error>.Ok(new WaitSet(new SafeWaitSetHandle(waitsetHandle)));
    }

    /// <summary>
    /// Disposes of the WaitSetBuilder.
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
            throw new ObjectDisposedException(nameof(WaitSetBuilder));
    }
}