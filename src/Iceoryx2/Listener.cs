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

namespace Iceoryx2;

/// <summary>
/// A listener that can receive event notifications from notifiers.
/// Listeners receive lightweight event IDs without payload data.
/// </summary>
public sealed class Listener : IDisposable
{
    private SafeListenerHandle _handle;
    private bool _disposed;

    internal Listener(SafeListenerHandle handle)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
    }

    /// <summary>
    /// Tries to receive an event without blocking.
    /// </summary>
    /// <returns>
    /// On success, returns the EventId if one was received, or null if no event was available.
    /// On error, returns an error code.
    /// </returns>
    public Result<EventId?, Iox2Error> TryWait()
    {
        ThrowIfDisposed();

        try
        {
            var listenerHandle = _handle.DangerousGetHandle();
            var result = Native.Iox2NativeMethods.iox2_listener_try_wait_one(
                ref listenerHandle,
                out var nativeEventId,
                out var hasReceivedOne);

            if (result != Native.Iox2NativeMethods.IOX2_OK)
                return Result<EventId?, Iox2Error>.Err(Iox2Error.WaitFailed);

            if (!hasReceivedOne)
                return Result<EventId?, Iox2Error>.Ok(null);

            var eventId = EventId.FromNative(nativeEventId);
            return Result<EventId?, Iox2Error>.Ok(eventId);
        }
        catch (Exception)
        {
            return Result<EventId?, Iox2Error>.Err(Iox2Error.WaitFailed);
        }
    }

    /// <summary>
    /// Waits for an event with a timeout.
    /// </summary>
    /// <param name="timeout">The maximum time to wait for an event.</param>
    /// <returns>
    /// On success, returns the EventId if one was received, or null if the timeout elapsed.
    /// On error, returns an error code.
    /// </returns>
    public Result<EventId?, Iox2Error> TimedWait(TimeSpan timeout)
    {
        ThrowIfDisposed();

        try
        {
            var listenerHandle = _handle.DangerousGetHandle();
            var seconds = (ulong)timeout.TotalSeconds;
            var nanoseconds = (uint)((timeout.TotalSeconds - seconds) * 1_000_000_000);

            var result = Native.Iox2NativeMethods.iox2_listener_timed_wait_one(
                ref listenerHandle,
                out var nativeEventId,
                out var hasReceivedOne,
                seconds,
                nanoseconds);

            if (result != Native.Iox2NativeMethods.IOX2_OK)
                return Result<EventId?, Iox2Error>.Err(Iox2Error.WaitFailed);

            if (!hasReceivedOne)
                return Result<EventId?, Iox2Error>.Ok(null);

            var eventId = EventId.FromNative(nativeEventId);
            return Result<EventId?, Iox2Error>.Ok(eventId);
        }
        catch (Exception)
        {
            return Result<EventId?, Iox2Error>.Err(Iox2Error.WaitFailed);
        }
    }

    /// <summary>
    /// Asynchronously waits for an event with a timeout.
    /// This method offloads the blocking native call to a background thread.
    /// </summary>
    /// <param name="timeout">The maximum time to wait for an event.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the wait operation.</param>
    /// <returns>
    /// On success, returns the EventId if one was received, or null if the timeout elapsed.
    /// On error, returns an error code.
    /// </returns>
    public Task<Result<EventId?, Iox2Error>> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TimedWait(timeout);
        }, cancellationToken);
    }

    /// <summary>
    /// Asynchronously waits for an event indefinitely.
    /// This method offloads the blocking native call to a background thread.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token to cancel the wait operation.</param>
    /// <returns>
    /// On success, returns the received EventId.
    /// On error, returns an error code.
    /// </returns>
    public Task<Result<EventId, Iox2Error>> WaitAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return BlockingWait();
        }, cancellationToken);
    }

    /// <summary>
    /// Blocks until an event is received.
    /// Consider using WaitAsync() for better thread pool utilization.
    /// </summary>
    /// <returns>
    /// On success, returns the received EventId.
    /// On error, returns an error code.
    /// </returns>
    public Result<EventId, Iox2Error> BlockingWait()
    {
        ThrowIfDisposed();

        try
        {
            var listenerHandle = _handle.DangerousGetHandle();
            var result = Native.Iox2NativeMethods.iox2_listener_blocking_wait_one(
                ref listenerHandle,
                out var nativeEventId,
                out var hasReceivedOne);

            if (result != Native.Iox2NativeMethods.IOX2_OK)
                return Result<EventId, Iox2Error>.Err(Iox2Error.WaitFailed);

            // Blocking wait should always receive an event or return an error
            if (!hasReceivedOne)
                return Result<EventId, Iox2Error>.Err(Iox2Error.WaitFailed);

            var eventId = EventId.FromNative(nativeEventId);
            return Result<EventId, Iox2Error>.Ok(eventId);
        }
        catch (Exception)
        {
            return Result<EventId, Iox2Error>.Err(Iox2Error.WaitFailed);
        }
    }

    /// <summary>
    /// Gets the internal handle (for internal use by WaitSet).
    /// </summary>
    internal IntPtr GetHandle()
    {
        ThrowIfDisposed();
        return _handle.DangerousGetHandle();
    }

    /// <summary>
    /// Disposes of the resources used by the Listener instance.
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
            throw new ObjectDisposedException(nameof(Listener));
    }
}