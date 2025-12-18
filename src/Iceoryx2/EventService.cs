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
/// Represents an event service in the iceoryx2 system.
/// Event services are used for lightweight notification-based communication.
/// </summary>
public sealed class EventService : IDisposable
{
    private SafeEventServiceHandle _handle;
    private bool _disposed;

    internal EventService(SafeEventServiceHandle handle)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
    }

    /// <summary>
    /// Creates a notifier for this event service.
    /// </summary>
    /// <param name="defaultEventId">Optional default event ID to use when calling Notify() without an explicit ID.</param>
    /// <returns>A Result containing the Notifier on success, or an error on failure.</returns>
    public Result<Notifier, Iox2Error> CreateNotifier(EventId? defaultEventId = null)
    {
        ThrowIfDisposed();

        try
        {
            // Create notifier builder - pass by reference for handle
            var portFactoryHandle = _handle.DangerousGetHandle();
            var notifierBuilderHandle = Native.Iox2NativeMethods.iox2_port_factory_event_notifier_builder(
                ref portFactoryHandle,  // Pass by reference - C expects pointer to handle
                IntPtr.Zero);  // NULL - let C allocate the struct

            if (notifierBuilderHandle == IntPtr.Zero)
                return Result<Notifier, Iox2Error>.Err(Iox2Error.NotifierCreationFailed);

            // Set default event ID if provided
            if (defaultEventId.HasValue)
            {
                var nativeEventId = defaultEventId.Value.ToNative();
                Native.Iox2NativeMethods.iox2_port_factory_notifier_builder_set_default_event_id(
                    ref notifierBuilderHandle,
                    ref nativeEventId);
            }

            // Create notifier - pass NULL to let C allocate on heap
            var result = Native.Iox2NativeMethods.iox2_port_factory_notifier_builder_create(
                notifierBuilderHandle,
                IntPtr.Zero,  // NULL - let C allocate the struct
                out var notifierHandle);

            if (result != Native.Iox2NativeMethods.IOX2_OK || notifierHandle == IntPtr.Zero)
                return Result<Notifier, Iox2Error>.Err(Iox2Error.NotifierCreationFailed);

            var handle = new SafeNotifierHandle(notifierHandle);
            var notifier = new Notifier(handle);

            return Result<Notifier, Iox2Error>.Ok(notifier);
        }
        catch (Exception)
        {
            return Result<Notifier, Iox2Error>.Err(Iox2Error.NotifierCreationFailed);
        }
    }

    /// <summary>
    /// Creates a listener for this event service.
    /// </summary>
    /// <returns>A Result containing the Listener on success, or an error on failure.</returns>
    public Result<Listener, Iox2Error> CreateListener()
    {
        ThrowIfDisposed();

        try
        {
            // Create listener builder - pass by reference for handle
            var portFactoryHandle = _handle.DangerousGetHandle();
            var listenerBuilderHandle = Native.Iox2NativeMethods.iox2_port_factory_event_listener_builder(
                ref portFactoryHandle,  // Pass by reference - C expects pointer to handle
                IntPtr.Zero);  // NULL - let C allocate the struct

            if (listenerBuilderHandle == IntPtr.Zero)
                return Result<Listener, Iox2Error>.Err(Iox2Error.ListenerCreationFailed);

            // Create listener - pass NULL to let C allocate on heap
            var result = Native.Iox2NativeMethods.iox2_port_factory_listener_builder_create(
                listenerBuilderHandle,
                IntPtr.Zero,  // NULL - let C allocate the struct
                out var listenerHandle);

            if (result != Native.Iox2NativeMethods.IOX2_OK || listenerHandle == IntPtr.Zero)
                return Result<Listener, Iox2Error>.Err(Iox2Error.ListenerCreationFailed);

            var handle = new SafeListenerHandle(listenerHandle);
            var listener = new Listener(handle);

            return Result<Listener, Iox2Error>.Ok(listener);
        }
        catch (Exception)
        {
            return Result<Listener, Iox2Error>.Err(Iox2Error.ListenerCreationFailed);
        }
    }

    /// <summary>
    /// Disposes of the resources used by the EventService instance.
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
            throw new ObjectDisposedException(nameof(EventService));
    }
}