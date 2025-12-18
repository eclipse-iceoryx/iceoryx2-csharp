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
/// Builder for creating subscribers with custom buffer configuration.
/// </summary>
public sealed class SubscriberBuilder
{
    private readonly Service _service;
    private ulong? _bufferSize;

    internal SubscriberBuilder(Service service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    /// <summary>
    /// Sets the buffer size for the subscriber.
    /// This defines how many samples the subscriber can buffer internally.
    /// When set to a value >= history_size, the subscriber will receive historical samples upon connection.
    /// </summary>
    /// <param name="value">Buffer size (minimum: 1)</param>
    /// <returns>This builder for method chaining</returns>
    public SubscriberBuilder BufferSize(ulong value)
    {
        _bufferSize = value;
        return this;
    }

    /// <summary>
    /// Creates the subscriber with the configured settings.
    /// </summary>
    /// <returns>Result containing the created Subscriber or an error</returns>
    public Result<Subscriber, Iox2Error> Create()
    {
        try
        {
            // Create subscriber builder - pass by reference for handle
            var portFactoryHandle = _service.GetHandle().DangerousGetHandle();
            var subscriberBuilderHandle = Native.Iox2NativeMethods.iox2_port_factory_pub_sub_subscriber_builder(
                ref portFactoryHandle,  // Pass by reference - C expects pointer to handle
                IntPtr.Zero);  // NULL - let C allocate the struct

            if (subscriberBuilderHandle == IntPtr.Zero)
                return Result<Subscriber, Iox2Error>.Err(Iox2Error.SubscriberCreationFailed);

            // Apply buffer size if specified
            if (_bufferSize.HasValue)
            {
                Native.Iox2NativeMethods.iox2_port_factory_subscriber_builder_set_buffer_size(
                    ref subscriberBuilderHandle, new UIntPtr(_bufferSize.Value));
            }

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
}