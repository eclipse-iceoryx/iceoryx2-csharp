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
/// Builder for publish-subscribe services.
/// </summary>
public sealed class PublishSubscribeServiceBuilder<T> where T : unmanaged
{
    private readonly Node _node;
    private string? _serviceName;
    private ulong? _maxSubscribers;
    private ulong? _maxPublishers;
    private ulong? _subscriberMaxBufferSize;
    private ulong? _subscriberMaxBorrowedSamples;
    private ulong? _historySize;
    private bool? _enableSafeOverflow;

    internal PublishSubscribeServiceBuilder(Node node)
    {
        _node = node ?? throw new ArgumentNullException(nameof(node));
    }

    /// <summary>
    /// Sets the maximum number of subscribers that can connect to this service.
    /// </summary>
    /// <param name="value">Maximum number of subscribers (default: 8)</param>
    /// <returns>This builder for method chaining</returns>
    public PublishSubscribeServiceBuilder<T> MaxSubscribers(ulong value)
    {
        _maxSubscribers = value;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of publishers that can connect to this service.
    /// </summary>
    /// <param name="value">Maximum number of publishers (default: 2)</param>
    /// <returns>This builder for method chaining</returns>
    public PublishSubscribeServiceBuilder<T> MaxPublishers(ulong value)
    {
        _maxPublishers = value;
        return this;
    }

    /// <summary>
    /// Sets the maximum buffer size for each subscriber.
    /// This defines how many samples a subscriber can store in its internal buffer.
    /// </summary>
    /// <param name="value">Maximum buffer size per subscriber (default: 2)</param>
    /// <returns>This builder for method chaining</returns>
    public PublishSubscribeServiceBuilder<T> SubscriberMaxBufferSize(ulong value)
    {
        _subscriberMaxBufferSize = value;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of samples a subscriber can borrow simultaneously.
    /// </summary>
    /// <param name="value">Maximum borrowed samples per subscriber (default: 2)</param>
    /// <returns>This builder for method chaining</returns>
    public PublishSubscribeServiceBuilder<T> SubscriberMaxBorrowedSamples(ulong value)
    {
        _subscriberMaxBorrowedSamples = value;
        return this;
    }

    /// <summary>
    /// Sets the history size for late-joining subscribers.
    /// When a subscriber connects, it can receive up to this many historical samples.
    /// </summary>
    /// <param name="value">History size (default: 0)</param>
    /// <returns>This builder for method chaining</returns>
    public PublishSubscribeServiceBuilder<T> HistorySize(ulong value)
    {
        _historySize = value;
        return this;
    }

    /// <summary>
    /// Enables or disables safe overflow behavior.
    /// When enabled and a subscriber's buffer is full, the oldest sample will be overridden by the newest one.
    /// When disabled, the publisher will block or apply the unable-to-deliver strategy.
    /// </summary>
    /// <param name="value">True to enable safe overflow, false to disable (default: true)</param>
    /// <returns>This builder for method chaining</returns>
    public PublishSubscribeServiceBuilder<T> EnableSafeOverflow(bool value)
    {
        _enableSafeOverflow = value;
        return this;
    }

    /// <summary>
    /// Opens an existing service or creates a new one with the specified name.
    /// </summary>
    public Result<Service, Iox2Error> Open(string serviceName)
    {
        _serviceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));

        try
        {
            // Create service name
            var serviceNameBytes = System.Text.Encoding.UTF8.GetByteCount(_serviceName);

            var result = Native.Iox2NativeMethods.iox2_service_name_new(
                IntPtr.Zero,  // pass IntPtr.Zero to use default storage allocation
                _serviceName,
                serviceNameBytes,
                out var serviceNameHandle);

            if (result != Native.Iox2NativeMethods.IOX2_OK)
                return Result<Service, Iox2Error>.Err(Iox2Error.ServiceCreationFailed);

            // Get service name ptr for builder
            var serviceNamePtr = Native.Iox2NativeMethods.iox2_cast_service_name_ptr(serviceNameHandle);

            // Create service builder - pass NULL to let C allocate on heap
            var nodeHandle = _node._handle.DangerousGetHandle();
            var serviceBuilderHandle = Native.Iox2NativeMethods.iox2_node_service_builder(
                ref nodeHandle,  // Pass by reference - C expects pointer to handle
                IntPtr.Zero,  // NULL - let C allocate the struct
                serviceNamePtr);

            // Clean up service name
            Native.Iox2NativeMethods.iox2_service_name_drop(serviceNameHandle);

            if (serviceBuilderHandle == IntPtr.Zero)
                return Result<Service, Iox2Error>.Err(Iox2Error.ServiceCreationFailed);

            // Get pub/sub builder
            var pubSubBuilderHandle = Native.Iox2NativeMethods.iox2_service_builder_pub_sub(serviceBuilderHandle);

            // Apply QoS settings if specified
            if (_maxSubscribers.HasValue)
            {
                Native.Iox2NativeMethods.iox2_service_builder_pub_sub_set_max_subscribers(
                    ref pubSubBuilderHandle, new UIntPtr(_maxSubscribers.Value));
            }

            if (_maxPublishers.HasValue)
            {
                Native.Iox2NativeMethods.iox2_service_builder_pub_sub_set_max_publishers(
                    ref pubSubBuilderHandle, new UIntPtr(_maxPublishers.Value));
            }

            if (_subscriberMaxBufferSize.HasValue)
            {
                Native.Iox2NativeMethods.iox2_service_builder_pub_sub_set_subscriber_max_buffer_size(
                    ref pubSubBuilderHandle, new UIntPtr(_subscriberMaxBufferSize.Value));
            }

            if (_subscriberMaxBorrowedSamples.HasValue)
            {
                Native.Iox2NativeMethods.iox2_service_builder_pub_sub_set_subscriber_max_borrowed_samples(
                    ref pubSubBuilderHandle, new UIntPtr(_subscriberMaxBorrowedSamples.Value));
            }

            if (_historySize.HasValue)
            {
                Native.Iox2NativeMethods.iox2_service_builder_pub_sub_set_history_size(
                    ref pubSubBuilderHandle, new UIntPtr(_historySize.Value));
            }

            if (_enableSafeOverflow.HasValue)
            {
                Native.Iox2NativeMethods.iox2_service_builder_pub_sub_set_enable_safe_overflow(
                    ref pubSubBuilderHandle, _enableSafeOverflow.Value);
            }

            // Set payload type details
            // Use Rust-compatible type names for cross-language interoperability
            var typeName = ServiceBuilder.GetRustCompatibleTypeName<T>();
            unsafe
            {
                var typeSize = (ulong)sizeof(T);
                // Calculate proper alignment - for primitive types it's the size, for structs we use Marshal.StructLayout or default to pointer size
                ulong typeAlignment;
                if (typeof(T).IsPrimitive)
                {
                    typeAlignment = typeSize;
                }
                else
                {
                    // For structs, check if there's a StructLayout attribute specifying Pack
                    var layoutAttr = typeof(T).StructLayoutAttribute;
                    if (layoutAttr != null && layoutAttr.Pack > 0)
                    {
                        typeAlignment = (ulong)layoutAttr.Pack;
                    }
                    else
                    {
                        // Default to pointer size for alignment
                        typeAlignment = (ulong)IntPtr.Size;
                    }
                }

                var typeResult = Native.Iox2NativeMethods.iox2_service_builder_pub_sub_set_payload_type_details(
                    ref pubSubBuilderHandle,  // Pass by reference - C expects pointer to handle
                    Native.Iox2NativeMethods.iox2_type_variant_e.FIXED_SIZE,
                    typeName,
                    System.Text.Encoding.UTF8.GetByteCount(typeName),
                    typeSize,
                    typeAlignment);

                if (typeResult != Native.Iox2NativeMethods.IOX2_OK)
                    return Result<Service, Iox2Error>.Err(Iox2Error.ServiceCreationFailed);
            }

            // Open or create the service - pass NULL to let C allocate on heap
            var openResult = Native.Iox2NativeMethods.iox2_service_builder_pub_sub_open_or_create(
                pubSubBuilderHandle,
                IntPtr.Zero,  // NULL - let C allocate the struct
                out var portFactoryHandle);

            if (openResult != Native.Iox2NativeMethods.IOX2_OK)
                return Result<Service, Iox2Error>.Err(Iox2Error.ServiceCreationFailed);

            if (portFactoryHandle == IntPtr.Zero)
                return Result<Service, Iox2Error>.Err(Iox2Error.ServiceCreationFailed);

            var handle = new SafeServiceHandle(portFactoryHandle);
            var service = new Service(handle);

            return Result<Service, Iox2Error>.Ok(service);
        }
        catch (Exception)
        {
            return Result<Service, Iox2Error>.Err(Iox2Error.ServiceCreationFailed);
        }
    }
}