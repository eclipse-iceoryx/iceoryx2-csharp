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
/// Represents a mutable request message that can be written to and sent to a server.
/// </summary>
/// <typeparam name="TRequest">The type of the request payload.</typeparam>
/// <typeparam name="TResponse">The type of the response payload.</typeparam>
public sealed class RequestMut<TRequest, TResponse> : IDisposable
    where TRequest : unmanaged
    where TResponse : unmanaged
{
    private IntPtr _handle;
    private bool _disposed;

    internal RequestMut(IntPtr handle)
    {
        _handle = handle;
    }

    /// <summary>
    /// Gets or sets the request payload data.
    /// </summary>
    public unsafe TRequest Payload
    {
        get
        {
            ThrowIfDisposed();

            iox2_request_mut_payload_mut(ref _handle, out var payloadPtr, out var payloadLen);

            if (payloadPtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to get request payload");
            }

            return *(TRequest*)payloadPtr;
        }
        set
        {
            ThrowIfDisposed();

            iox2_request_mut_payload_mut(ref _handle, out var payloadPtr, out var payloadLen);

            if (payloadPtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to get request payload");
            }

            *(TRequest*)payloadPtr = value;
        }
    }

    /// <summary>
    /// Sends the request to the server and returns a pending response handle.
    /// After calling this method, the RequestMut is consumed and should not be used again.
    /// </summary>
    /// <returns>A Result containing the pending response or an error.</returns>
    public Result<PendingResponse<TResponse>, Iox2Error> Send()
    {
        ThrowIfDisposed();

        var result = iox2_request_mut_send(
            _handle,
            IntPtr.Zero,
            out var pendingResponseHandle);

        if (result != IOX2_OK)
        {
            return Result<PendingResponse<TResponse>, Iox2Error>.Err(Iox2Error.RequestSendFailed);
        }

        // Mark as disposed since the handle is consumed by send
        _disposed = true;
        _handle = IntPtr.Zero;

        return Result<PendingResponse<TResponse>, Iox2Error>.Ok(new PendingResponse<TResponse>(pendingResponseHandle));
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(RequestMut<TRequest, TResponse>));
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="RequestMut{TRequest, TResponse}"/>.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            if (_handle != IntPtr.Zero)
            {
                iox2_request_mut_drop(_handle);
                _handle = IntPtr.Zero;
            }
            _disposed = true;
        }
    }
}