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
using System.Runtime.InteropServices;
using static Iceoryx2.Native.Iox2NativeMethods;

namespace Iceoryx2.RequestResponse;

/// <summary>
/// Represents a received request from a client in a request-response communication pattern.
/// Provides access to the request payload and methods to send responses back to the client.
/// </summary>
/// <typeparam name="TRequest">The type of the request payload.</typeparam>
/// <typeparam name="TResponse">The type of the response payload.</typeparam>
public sealed class Request<TRequest, TResponse> : IDisposable
    where TRequest : unmanaged
    where TResponse : unmanaged
{
    private IntPtr _handle;
    private bool _disposed;

    internal Request(IntPtr handle)
    {
        _handle = handle;
    }

    /// <summary>
    /// Gets the request payload data.
    /// </summary>
    public unsafe TRequest Payload
    {
        get
        {
            ThrowIfDisposed();

            iox2_active_request_payload(ref _handle, out var payloadPtr, out var payloadLen);

            if (payloadPtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to get request payload");
            }

            return *(TRequest*)payloadPtr;
        }
    }

    /// <summary>
    /// Loans a response message to send back to the client.
    /// </summary>
    /// <returns>A Result containing the response message or an error.</returns>
    public Result<ResponseMut<TResponse>, Iox2Error> LoanResponse()
    {
        ThrowIfDisposed();

        var result = iox2_active_request_loan_slice_uninit(
            ref _handle,
            IntPtr.Zero,
            out var responseHandle,
            new UIntPtr(1));

        if (result != IOX2_OK)
        {
            return Result<ResponseMut<TResponse>, Iox2Error>.Err(Iox2Error.ResponseLoanFailed);
        }

        return Result<ResponseMut<TResponse>, Iox2Error>.Ok(new ResponseMut<TResponse>(responseHandle));
    }

    /// <summary>
    /// Sends a response by copying the provided data.
    /// This is a convenience method that loans, writes, and sends in one operation.
    /// </summary>
    /// <param name="response">The response data to send.</param>
    /// <returns>A Result indicating success or an error.</returns>
    public unsafe Result<Unit, Iox2Error> SendCopyResponse(TResponse response)
    {
        ThrowIfDisposed();

        var result = iox2_active_request_send_copy(
            ref _handle,
            new IntPtr(&response),
            new UIntPtr((uint)Marshal.SizeOf<TResponse>()),
            new UIntPtr(1));

        if (result != IOX2_OK)
        {
            return Result<Unit, Iox2Error>.Err(Iox2Error.ResponseSendFailed);
        }

        return Result<Unit, Iox2Error>.Ok(new Unit());
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(Request<TRequest, TResponse>));
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="Request{TRequest, TResponse}"/>.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            if (_handle != IntPtr.Zero)
            {
                iox2_active_request_drop(_handle);
                _handle = IntPtr.Zero;
            }
            _disposed = true;
        }
    }
}