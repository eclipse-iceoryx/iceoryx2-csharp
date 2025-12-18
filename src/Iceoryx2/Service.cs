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
/// Represents a service in the iceoryx2 system.
/// Services are created with a specific messaging pattern (e.g., publish-subscribe).
/// </summary>
public sealed class Service : IDisposable
{
    private SafeServiceHandle _handle;
    private bool _disposed;

    internal Service(SafeServiceHandle handle)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
    }

    /// <summary>
    /// Gets a builder for creating publishers with custom QoS settings.
    /// </summary>
    /// <returns>A PublisherBuilder instance for configuring and creating a publisher</returns>
    public PublisherBuilder PublisherBuilder()
    {
        ThrowIfDisposed();
        return new PublisherBuilder(this);
    }

    /// <summary>
    /// Gets a builder for creating subscribers with custom buffer configuration.
    /// </summary>
    /// <returns>A SubscriberBuilder instance for configuring and creating a subscriber</returns>
    public SubscriberBuilder SubscriberBuilder()
    {
        ThrowIfDisposed();
        return new SubscriberBuilder(this);
    }

    /// <summary>
    /// Creates a publisher for this service with default settings.
    /// For custom QoS settings, use PublisherBuilder() instead.
    /// </summary>
    public Result<Publisher, Iox2Error> CreatePublisher()
    {
        ThrowIfDisposed();

        try
        {
            // Create publisher builder - pass by reference for handle
            var portFactoryHandle = _handle.DangerousGetHandle();
            var publisherBuilderHandle = Native.Iox2NativeMethods.iox2_port_factory_pub_sub_publisher_builder(
                ref portFactoryHandle,  // Pass by reference - C expects pointer to handle
                IntPtr.Zero);  // NULL - let C allocate the struct

            if (publisherBuilderHandle == IntPtr.Zero)
                return Result<Publisher, Iox2Error>.Err(Iox2Error.PublisherCreationFailed);

            // Create publisher - pass NULL to let C allocate on heap
            var result = Native.Iox2NativeMethods.iox2_port_factory_publisher_builder_create(
                publisherBuilderHandle,
                IntPtr.Zero,  // NULL - let C allocate the struct
                out var publisherHandle);

            if (result != Native.Iox2NativeMethods.IOX2_OK || publisherHandle == IntPtr.Zero)
                return Result<Publisher, Iox2Error>.Err(Iox2Error.PublisherCreationFailed);

            var handle = new SafePublisherHandle(publisherHandle);
            var publisher = new Publisher(handle);

            return Result<Publisher, Iox2Error>.Ok(publisher);
        }
        catch (Exception)
        {
            return Result<Publisher, Iox2Error>.Err(Iox2Error.PublisherCreationFailed);
        }
    }

    /// <summary>
    /// Creates a subscriber for this service with default settings.
    /// For custom buffer configuration, use SubscriberBuilder() instead.
    /// </summary>
    public Result<Subscriber, Iox2Error> CreateSubscriber()
    {
        ThrowIfDisposed();

        try
        {
            // Create subscriber builder - pass by reference for handle
            var portFactoryHandle = _handle.DangerousGetHandle();
            var subscriberBuilderHandle = Native.Iox2NativeMethods.iox2_port_factory_pub_sub_subscriber_builder(
                ref portFactoryHandle,  // Pass by reference - C expects pointer to handle
                IntPtr.Zero);  // NULL - let C allocate the struct

            if (subscriberBuilderHandle == IntPtr.Zero)
                return Result<Subscriber, Iox2Error>.Err(Iox2Error.SubscriberCreationFailed);

            // Create subscriber - pass NULL to let C allocate on heap
            var result = Native.Iox2NativeMethods.iox2_port_factory_subscriber_builder_create(
                subscriberBuilderHandle,
                IntPtr.Zero,  // NULL - let C allocate the struct
                out var subscriberHandle);

            if (result != Native.Iox2NativeMethods.IOX2_OK || subscriberHandle == IntPtr.Zero)
                return Result<Subscriber, Iox2Error>.Err(Iox2Error.SubscriberCreationFailed);

            var handle = new SafeSubscriberHandle(subscriberHandle);
            var subscriber = new Subscriber(handle);

            return Result<Subscriber, Iox2Error>.Ok(subscriber);
        }
        catch (Exception)
        {
            return Result<Subscriber, Iox2Error>.Err(Iox2Error.SubscriberCreationFailed);
        }
    }

    /// <summary>
    /// Releases all resources used by the service instance.
    /// This method should be called to clean up any unmanaged resources
    /// and mark the object as disposed to prevent further usage.
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
            throw new ObjectDisposedException(nameof(Service));
    }

    /// <summary>
    /// Internal method to get the underlying handle for builder classes.
    /// </summary>
    internal SafeServiceHandle GetHandle()
    {
        ThrowIfDisposed();
        return _handle;
    }
}