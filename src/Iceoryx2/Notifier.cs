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
/// A notifier that can send event notifications to listeners.
/// Notifiers send lightweight event IDs without payload data.
/// </summary>
public sealed class Notifier : IDisposable
{
    private SafeNotifierHandle _handle;
    private bool _disposed;

    internal Notifier(SafeNotifierHandle handle)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
    }

    /// <summary>
    /// Notifies all connected listeners with the default event ID.
    /// </summary>
    /// <returns>
    /// On success, returns Unit. The number of notified listeners is not returned.
    /// On error, returns an error code.
    /// </returns>
    public Result<Unit, Iox2Error> Notify()
    {
        ThrowIfDisposed();

        try
        {
            var notifierHandle = _handle.DangerousGetHandle();
            var result = Native.Iox2NativeMethods.iox2_notifier_notify(
                ref notifierHandle,
                IntPtr.Zero);  // Pass NULL for listener count

            if (result != Native.Iox2NativeMethods.IOX2_OK)
                return Result<Unit, Iox2Error>.Err(Iox2Error.NotifyFailed);

            return Result<Unit, Iox2Error>.Ok(Unit.Value);
        }
        catch (Exception)
        {
            return Result<Unit, Iox2Error>.Err(Iox2Error.NotifyFailed);
        }
    }

    /// <summary>
    /// Notifies all connected listeners with a custom event ID.
    /// </summary>
    /// <param name="eventId">The event ID to send to listeners.</param>
    /// <returns>
    /// On success, returns Unit. The number of notified listeners is not returned.
    /// On error, returns an error code.
    /// </returns>
    public Result<Unit, Iox2Error> Notify(EventId eventId)
    {
        ThrowIfDisposed();

        try
        {
            var notifierHandle = _handle.DangerousGetHandle();
            var nativeEventId = eventId.ToNative();
            var result = Native.Iox2NativeMethods.iox2_notifier_notify_with_custom_event_id(
                ref notifierHandle,
                ref nativeEventId,
                IntPtr.Zero);  // Pass NULL for listener count

            if (result != Native.Iox2NativeMethods.IOX2_OK)
                return Result<Unit, Iox2Error>.Err(Iox2Error.NotifyFailed);

            return Result<Unit, Iox2Error>.Ok(Unit.Value);
        }
        catch (Exception)
        {
            return Result<Unit, Iox2Error>.Err(Iox2Error.NotifyFailed);
        }
    }

    /// <summary>
    /// Disposes of the resources used by the Notifier instance.
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
            throw new ObjectDisposedException(nameof(Notifier));
    }
}