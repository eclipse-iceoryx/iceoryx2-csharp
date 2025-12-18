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
using System.Threading;
using System.Threading.Tasks;
using static Iceoryx2.Native.Iox2NativeMethods;

namespace Iceoryx2.RequestResponse;

/// <summary>
/// Represents a pending response from a server after sending a request.
/// Provides methods to wait for and receive the response.
/// </summary>
/// <typeparam name="TResponse">The type of the response payload.</typeparam>
public sealed class PendingResponse<TResponse> : IDisposable
    where TResponse : unmanaged
{
    private readonly SafePendingResponseHandle _handle;
    private bool _disposed;

    internal PendingResponse(IntPtr handle)
    {
        _handle = new SafePendingResponseHandle(handle);
    }

    /// <summary>
    /// Attempts to receive a response without blocking.
    /// Returns null if no response is available yet.
    /// </summary>
    /// <returns>A Result containing the response if available (null if no response yet), or an error.</returns>
    public unsafe Result<Response<TResponse>?, Iox2Error> Receive()
    {
        ThrowIfDisposed();

        bool success = false;
        _handle.DangerousAddRef(ref success);
        if (!success)
        {
            return Result<Response<TResponse>?, Iox2Error>.Err(Iox2Error.ResponseReceiveFailed);
        }

        try
        {
            var handlePtr = _handle.DangerousGetHandle();
            var result = iox2_pending_response_receive(
                ref handlePtr,
                IntPtr.Zero,
                out var responseHandle);

            if (result != IOX2_OK)
            {
                return Result<Response<TResponse>?, Iox2Error>.Err(Iox2Error.ResponseReceiveFailed);
            }

            if (responseHandle == IntPtr.Zero)
            {
                return Result<Response<TResponse>?, Iox2Error>.Ok(null);
            }

            return Result<Response<TResponse>?, Iox2Error>.Ok(new Response<TResponse>(responseHandle));
        }
        finally
        {
            _handle.DangerousRelease();
        }
    }

    /// <summary>
    /// Attempts to receive a response without blocking.
    /// This is an alias for Receive() for compatibility.
    /// </summary>
    /// <returns>A Result containing the response if available (null if no response yet), or an error.</returns>
    public Result<Response<TResponse>?, Iox2Error> TryReceive()
    {
        return Receive();
    }

    /// <summary>
    /// Waits for a response with a timeout by polling.
    /// Note: This is implemented as a polling loop since the native API doesn't have timed receive.
    /// </summary>
    /// <param name="timeout">The maximum time to wait for a response.</param>
    /// <returns>A Result containing the response if received within timeout (null if timeout), or an error.</returns>
    public Result<Response<TResponse>?, Iox2Error> TimedReceive(TimeSpan timeout)
    {
        ThrowIfDisposed();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            var result = Receive();
            if (!result.IsOk)
            {
                return result;
            }

            var response = result.Unwrap();
            if (response != null)
            {
                return Result<Response<TResponse>?, Iox2Error>.Ok(response);
            }

            // Small sleep to avoid busy waiting
            System.Threading.Thread.Sleep(10);
        }

        return Result<Response<TResponse>?, Iox2Error>.Ok(null);
    }

    /// <summary>
    /// Asynchronously waits for a response with a timeout.
    /// This is the async version that yields to the thread pool instead of blocking.
    /// </summary>
    /// <param name="timeout">The maximum time to wait for a response.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the wait operation.</param>
    /// <returns>A Task containing a Result with the response if received within timeout (null if timeout), or an error.</returns>
    public async Task<Result<Response<TResponse>?, Iox2Error>> ReceiveAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = Receive();
            if (!result.IsOk)
            {
                return result;
            }

            var response = result.Unwrap();
            if (response != null)
            {
                return Result<Response<TResponse>?, Iox2Error>.Ok(response);
            }

            // Yield to thread pool instead of blocking
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
        }

        return Result<Response<TResponse>?, Iox2Error>.Ok(null);
    }

    /// <summary>
    /// Asynchronously waits for a response indefinitely.
    /// This is the async version that yields to the thread pool instead of blocking.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token to cancel the wait operation.</param>
    /// <returns>A Task containing a Result with the response or an error.</returns>
    public async Task<Result<Response<TResponse>, Iox2Error>> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = Receive();
            if (!result.IsOk)
            {
                return Result<Response<TResponse>, Iox2Error>.Err(Iox2Error.ResponseReceiveFailed);
            }

            var response = result.Unwrap();
            if (response != null)
            {
                return Result<Response<TResponse>, Iox2Error>.Ok(response);
            }

            // Yield to thread pool instead of blocking
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Blocks until a response is received by polling.
    /// Note: This is implemented as a polling loop since the native API doesn't have blocking receive.
    /// Consider using ReceiveAsync() for better thread pool utilization.
    /// </summary>
    /// <returns>A Result containing the response or an error.</returns>
    public Result<Response<TResponse>, Iox2Error> BlockingReceive()
    {
        ThrowIfDisposed();

        while (true)
        {
            var result = Receive();
            if (!result.IsOk)
            {
                return Result<Response<TResponse>, Iox2Error>.Err(Iox2Error.ResponseReceiveFailed);
            }

            var response = result.Unwrap();
            if (response != null)
            {
                return Result<Response<TResponse>, Iox2Error>.Ok(response);
            }

            // Small sleep to avoid busy waiting
            System.Threading.Thread.Sleep(10);
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PendingResponse<TResponse>));
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="PendingResponse{TResponse}"/>.
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