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
using static Iceoryx2.Native.Iox2NativeMethods;

namespace Iceoryx2.RequestResponse;

/// <summary>
/// Represents a client in the request-response pattern.
/// Clients send requests to servers and receive responses.
/// </summary>
/// <typeparam name="TRequest">The type of the request payload.</typeparam>
/// <typeparam name="TResponse">The type of the response payload.</typeparam>
public sealed class Client<TRequest, TResponse> : IDisposable
    where TRequest : unmanaged
    where TResponse : unmanaged
{
    private readonly SafeClientHandle _handle;
    private bool _disposed;

    internal Client(IntPtr handle)
    {
        _handle = new SafeClientHandle(handle);
    }

    /// <summary>
    /// Loans a request message that can be written to and sent.
    /// </summary>
    /// <returns>A Result containing the request message or an error.</returns>
    public Result<RequestMut<TRequest, TResponse>, Iox2Error> Loan()
    {
        ThrowIfDisposed();

        var handlePtr = _handle.DangerousGetHandle();
        var result = iox2_client_loan_slice_uninit(
            ref handlePtr,
            IntPtr.Zero,
            out var requestHandle,
            new UIntPtr(1));

        if (result != IOX2_OK)
        {
            return Result<RequestMut<TRequest, TResponse>, Iox2Error>.Err(Iox2Error.RequestLoanFailed);
        }

        return Result<RequestMut<TRequest, TResponse>, Iox2Error>.Ok(new RequestMut<TRequest, TResponse>(requestHandle));
    }

    /// <summary>
    /// Sends a request by copying the provided data.
    /// This is a convenience method that loans, writes, and sends in one operation.
    /// </summary>
    /// <param name="request">The request data to send.</param>
    /// <returns>A Result containing the pending response or an error.</returns>
    public unsafe Result<PendingResponse<TResponse>, Iox2Error> SendCopy(TRequest request)
    {
        ThrowIfDisposed();

        var handlePtr = _handle.DangerousGetHandle();
        var result = iox2_client_send_copy(
            ref handlePtr,
            new IntPtr(&request),
            new UIntPtr((uint)Marshal.SizeOf<TRequest>()),
            new UIntPtr(1),
            IntPtr.Zero,
            out var pendingResponseHandle);

        if (result != IOX2_OK)
        {
            return Result<PendingResponse<TResponse>, Iox2Error>.Err(Iox2Error.RequestSendFailed);
        }

        return Result<PendingResponse<TResponse>, Iox2Error>.Ok(new PendingResponse<TResponse>(pendingResponseHandle));
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(Client<TRequest, TResponse>));
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="Client{TRequest, TResponse}"/>.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _handle?.Dispose();
            _disposed = true;
        }
    }
}