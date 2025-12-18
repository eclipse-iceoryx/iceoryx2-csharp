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
/// Represents a request-response service port factory.
/// Provides methods to create clients and servers for request-response communication.
/// </summary>
/// <typeparam name="TRequest">The type of the request payload.</typeparam>
/// <typeparam name="TResponse">The type of the response payload.</typeparam>
public sealed class RequestResponseService<TRequest, TResponse> : IDisposable
    where TRequest : unmanaged
    where TResponse : unmanaged
{
    private readonly SafeRequestResponseServiceHandle _handle;
    private bool _disposed;

    internal RequestResponseService(IntPtr handle)
    {
        _handle = new SafeRequestResponseServiceHandle(handle);
    }

    /// <summary>
    /// Creates a new client for sending requests and receiving responses.
    /// </summary>
    /// <returns>A Result containing the client or an error.</returns>
    public Result<Client<TRequest, TResponse>, Iox2Error> CreateClient()
    {
        ThrowIfDisposed();

        var handlePtr = _handle.DangerousGetHandle();
        var clientBuilderHandle = iox2_port_factory_request_response_client_builder(
            ref handlePtr,
            IntPtr.Zero);

        if (clientBuilderHandle == IntPtr.Zero)
        {
            return Result<Client<TRequest, TResponse>, Iox2Error>.Err(Iox2Error.ClientCreationFailed);
        }

        var result = iox2_port_factory_client_builder_create(
            clientBuilderHandle,
            IntPtr.Zero,
            out var clientHandle);

        if (result != IOX2_OK)
        {
            return Result<Client<TRequest, TResponse>, Iox2Error>.Err(Iox2Error.ClientCreationFailed);
        }

        return Result<Client<TRequest, TResponse>, Iox2Error>.Ok(new Client<TRequest, TResponse>(clientHandle));
    }

    /// <summary>
    /// Creates a new server for receiving requests and sending responses.
    /// </summary>
    /// <returns>A Result containing the server or an error.</returns>
    public Result<Server<TRequest, TResponse>, Iox2Error> CreateServer()
    {
        ThrowIfDisposed();

        var handlePtr = _handle.DangerousGetHandle();
        var serverBuilderHandle = iox2_port_factory_request_response_server_builder(
            ref handlePtr,
            IntPtr.Zero);

        if (serverBuilderHandle == IntPtr.Zero)
        {
            return Result<Server<TRequest, TResponse>, Iox2Error>.Err(Iox2Error.ServerCreationFailed);
        }

        var result = iox2_port_factory_server_builder_create(
            serverBuilderHandle,
            IntPtr.Zero,
            out var serverHandle);

        if (result != IOX2_OK)
        {
            return Result<Server<TRequest, TResponse>, Iox2Error>.Err(Iox2Error.ServerCreationFailed);
        }

        return Result<Server<TRequest, TResponse>, Iox2Error>.Ok(new Server<TRequest, TResponse>(serverHandle));
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(RequestResponseService<TRequest, TResponse>));
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="RequestResponseService{TRequest, TResponse}"/>.
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