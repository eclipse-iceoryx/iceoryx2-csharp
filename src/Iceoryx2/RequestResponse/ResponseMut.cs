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

namespace Iceoryx2.RequestResponse;

/// <summary>
/// Represents a mutable response message that can be written to and sent back to a client.
/// </summary>
/// <typeparam name="TResponse">The type of the response payload.</typeparam>
public sealed class ResponseMut<TResponse> : IDisposable
    where TResponse : unmanaged
{
    private IntPtr _handle;
    private bool _disposed;

    internal ResponseMut(IntPtr handle)
    {
        _handle = handle;
    }

    /// <summary>
    /// Gets or sets the response payload data.
    /// </summary>
    public unsafe TResponse Payload
    {
        get
        {
            ThrowIfDisposed();

            iox2_response_mut_payload_mut(ref _handle, out var payloadPtr, out var payloadLen);

            if (payloadPtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to get response payload");
            }

            return *(TResponse*)payloadPtr;
        }
        set
        {
            ThrowIfDisposed();

            iox2_response_mut_payload_mut(ref _handle, out var payloadPtr, out var payloadLen);

            if (payloadPtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to get response payload");
            }

            *(TResponse*)payloadPtr = value;
        }
    }

    /// <summary>
    /// Sends the response back to the client.
    /// After calling this method, the ResponseMut is consumed and should not be used again.
    /// </summary>
    /// <returns>A Result indicating success or an error.</returns>
    public Result<Unit, Iox2Error> Send()
    {
        ThrowIfDisposed();

        var result = iox2_response_mut_send(_handle);

        if (result != IOX2_OK)
        {
            return Result<Unit, Iox2Error>.Err(Iox2Error.ResponseSendFailed);
        }

        // Mark as disposed since the handle is consumed by send
        _disposed = true;
        _handle = IntPtr.Zero;

        return Result<Unit, Iox2Error>.Ok(new Unit());
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ResponseMut<TResponse>));
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="ResponseMut{TResponse}"/>.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            if (_handle != IntPtr.Zero)
            {
                iox2_response_mut_drop(_handle);
                _handle = IntPtr.Zero;
            }
            _disposed = true;
        }
    }
}