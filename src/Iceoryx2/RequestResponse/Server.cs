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
using static Iceoryx2.Native.Iox2NativeMethods;

namespace Iceoryx2.RequestResponse;

/// <summary>
/// Represents a server in the request-response pattern.
/// Servers receive requests from clients and send back responses.
/// </summary>
/// <typeparam name="TRequest">The type of the request payload.</typeparam>
/// <typeparam name="TResponse">The type of the response payload.</typeparam>
public sealed class Server<TRequest, TResponse> : IDisposable
    where TRequest : unmanaged
    where TResponse : unmanaged
{
    private readonly SafeServerHandle _handle;
    private bool _disposed;

    internal Server(IntPtr handle)
    {
        _handle = new SafeServerHandle(handle);
    }

    /// <summary>
    /// Receives a request from a client, if available.
    /// </summary>
    /// <returns>A Result containing the request if available (null if no request), or an error.</returns>
    public Result<Request<TRequest, TResponse>?, Iox2Error> Receive()
    {
        ThrowIfDisposed();

        var handlePtr = _handle.DangerousGetHandle();
        var result = iox2_server_receive(
            ref handlePtr,
            IntPtr.Zero,
            out var requestHandle);

        if (result != IOX2_OK)
        {
            return Result<Request<TRequest, TResponse>?, Iox2Error>.Err(Iox2Error.ReceiveFailed);
        }

        if (requestHandle == IntPtr.Zero)
        {
            return Result<Request<TRequest, TResponse>?, Iox2Error>.Ok(null);
        }

        return Result<Request<TRequest, TResponse>?, Iox2Error>.Ok(new Request<TRequest, TResponse>(requestHandle));
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(Server<TRequest, TResponse>));
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="Server{TRequest, TResponse}"/>.
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