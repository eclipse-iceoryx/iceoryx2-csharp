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
using System.Runtime.InteropServices;

namespace Iceoryx2;

/// <summary>
/// Represents a data sample that can be sent or received.
/// </summary>
public sealed class Sample<T> : IDisposable where T : unmanaged
{
    private SafeSampleHandle _handle;
    private bool _disposed;

    internal Sample(SafeSampleHandle handle)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
    }

    /// <summary>
    /// Gets or sets the payload data.
    /// </summary>
    public unsafe T Payload
    {
        get
        {
            ThrowIfDisposed();
            var sampleHandle = _handle.DangerousGetHandle();
            IntPtr payloadPtr;

            if (_handle.IsMutable)
            {
                Native.Iox2NativeMethods.iox2_sample_mut_payload_mut_ptr(
                    ref sampleHandle,
                    out payloadPtr,
                    IntPtr.Zero);
            }
            else
            {
                Native.Iox2NativeMethods.iox2_sample_payload(
                    ref sampleHandle,
                    out payloadPtr,
                    out _);
            }

            if (payloadPtr == IntPtr.Zero)
                throw new InvalidOperationException("Failed to get sample payload");

            return Marshal.PtrToStructure<T>(payloadPtr);
        }
        set
        {
            ThrowIfDisposed();

            var sampleHandle = _handle.DangerousGetHandle();
            IntPtr payloadPtr;
            unsafe
            {
                // WORKAROUND: Pass NULL for number_of_elements because native code has a bug
                // where it accesses .local union variant even when service_type is IPC
                Native.Iox2NativeMethods.iox2_sample_mut_payload_mut_ptr(
                    ref sampleHandle,  // _ref type needs ref to pass pointer-to-pointer
                    out payloadPtr,
                    IntPtr.Zero);  // NULL - don't query element count due to native bug
            }

            if (payloadPtr == IntPtr.Zero)
                throw new InvalidOperationException("Failed to get sample payload");

            // Ensure we don't overwrite memory unexpectedly. Marshal the structure into a temporary
            // unmanaged buffer and then copy the bytes into the payload pointer returned by native.
            var structSize = Marshal.SizeOf<T>();
            // We loaned exactly 1 element, so available bytes = structSize
            var availableBytes = (ulong)structSize;

            var tmp = Marshal.AllocHGlobal(structSize);
            try
            {
                Marshal.StructureToPtr(value, tmp, false);
                unsafe
                {
                    Buffer.MemoryCopy(tmp.ToPointer(), payloadPtr.ToPointer(), (long)availableBytes, (long)structSize);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(tmp);
            }
        }
    }

    /// <summary>
    /// Gets a mutable reference to the payload data in shared memory.
    /// This allows zero-copy access to the data.
    /// Throws InvalidOperationException if the sample is read-only (received from a subscriber).
    /// </summary>
    public unsafe ref T GetPayloadRef()
    {
        ThrowIfDisposed();

        if (!_handle.IsMutable)
            throw new InvalidOperationException("Cannot get mutable reference to read-only sample. Use GetPayloadRefReadOnly() instead.");

        var sampleHandle = _handle.DangerousGetHandle();
        IntPtr payloadPtr;

        Native.Iox2NativeMethods.iox2_sample_mut_payload_mut_ptr(
            ref sampleHandle,
            out payloadPtr,
            IntPtr.Zero);

        if (payloadPtr == IntPtr.Zero)
            throw new InvalidOperationException("Failed to get sample payload");

        return ref System.Runtime.CompilerServices.Unsafe.AsRef<T>(payloadPtr.ToPointer());
    }

    /// <summary>
    /// Gets a read-only reference to the payload data in shared memory.
    /// This allows zero-copy access to the data.
    /// Works for both loaned samples (mutable) and received samples (read-only).
    /// </summary>
    public unsafe ref readonly T GetPayloadRefReadOnly()
    {
        ThrowIfDisposed();

        var sampleHandle = _handle.DangerousGetHandle();
        IntPtr payloadPtr;

        if (_handle.IsMutable)
        {
            Native.Iox2NativeMethods.iox2_sample_mut_payload_mut_ptr(
                ref sampleHandle,
                out payloadPtr,
                IntPtr.Zero);
        }
        else
        {
            Native.Iox2NativeMethods.iox2_sample_payload(
                ref sampleHandle,
                out payloadPtr,
                out _);
        }

        if (payloadPtr == IntPtr.Zero)
            throw new InvalidOperationException("Failed to get sample payload");

        return ref System.Runtime.CompilerServices.Unsafe.AsRef<T>(payloadPtr.ToPointer());
    }

    /// <summary>
    /// Sends the sample to all connected subscribers.
    /// </summary>
    public Result<Unit, Iox2Error> Send()
    {
        ThrowIfDisposed();

        try
        {
            var sampleHandle = _handle.DangerousGetHandle();

            var result = Native.Iox2NativeMethods.iox2_sample_mut_send(
                sampleHandle,
                IntPtr.Zero);

            if (result != Native.Iox2NativeMethods.IOX2_OK)
                return Result<Unit, Iox2Error>.Err(Iox2Error.SendFailed);

            // The handle is consumed by send
            _handle.SetHandleAsInvalid();
            _disposed = true;

            return Result<Unit, Iox2Error>.Ok(Unit.Value);
        }
        catch (Exception)
        {
            return Result<Unit, Iox2Error>.Err(Iox2Error.SendFailed);
        }
    }

    /// <summary>
    /// Releases the resources associated with the current instance of the Sample class.
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
            throw new ObjectDisposedException(nameof(Sample<T>));
    }
}