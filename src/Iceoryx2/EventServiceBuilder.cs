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
/// Builder for event services.
/// Event services enable lightweight notification-based communication via event IDs.
/// </summary>
public sealed class EventServiceBuilder
{
    private readonly Node _node;

    internal EventServiceBuilder(Node node)
    {
        _node = node ?? throw new ArgumentNullException(nameof(node));
    }

    /// <summary>
    /// Opens an existing event service or creates a new one with the specified name.
    /// </summary>
    /// <param name="serviceName">The name of the event service.</param>
    /// <returns>A Result containing the EventService on success, or an error on failure.</returns>
    public Result<EventService, Iox2Error> Open(string serviceName)
    {
        if (serviceName == null)
            throw new ArgumentNullException(nameof(serviceName));

        try
        {
            // Create service name
            var serviceNameBytes = System.Text.Encoding.UTF8.GetByteCount(serviceName);

            var result = Native.Iox2NativeMethods.iox2_service_name_new(
                IntPtr.Zero,  // pass IntPtr.Zero to use default storage allocation
                serviceName,
                serviceNameBytes,
                out var serviceNameHandle);

            if (result != Native.Iox2NativeMethods.IOX2_OK)
                return Result<EventService, Iox2Error>.Err(Iox2Error.EventServiceCreationFailed);

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
                return Result<EventService, Iox2Error>.Err(Iox2Error.EventServiceCreationFailed);

            // Get event builder
            var eventBuilderHandle = Native.Iox2NativeMethods.iox2_service_builder_event(serviceBuilderHandle);

            if (eventBuilderHandle == IntPtr.Zero)
                return Result<EventService, Iox2Error>.Err(Iox2Error.EventServiceCreationFailed);

            // Open or create the event service - pass NULL to let C allocate on heap
            var openResult = Native.Iox2NativeMethods.iox2_service_builder_event_open_or_create(
                eventBuilderHandle,
                IntPtr.Zero,  // NULL - let C allocate the struct
                out var portFactoryHandle);

            if (openResult != Native.Iox2NativeMethods.IOX2_OK)
                return Result<EventService, Iox2Error>.Err(Iox2Error.EventServiceCreationFailed);

            if (portFactoryHandle == IntPtr.Zero)
                return Result<EventService, Iox2Error>.Err(Iox2Error.EventServiceCreationFailed);

            var handle = new SafeEventServiceHandle(portFactoryHandle);
            var service = new EventService(handle);

            return Result<EventService, Iox2Error>.Ok(service);
        }
        catch (Exception)
        {
            return Result<EventService, Iox2Error>.Err(Iox2Error.EventServiceCreationFailed);
        }
    }

    /// <summary>
    /// Creates a new event service with the specified name.
    /// Fails if a service with the same name already exists.
    /// </summary>
    /// <param name="serviceName">The name of the event service.</param>
    /// <returns>A Result containing the EventService on success, or an error on failure.</returns>
    public Result<EventService, Iox2Error> Create(string serviceName)
    {
        if (serviceName == null)
            throw new ArgumentNullException(nameof(serviceName));

        try
        {
            // Create service name
            var serviceNameBytes = System.Text.Encoding.UTF8.GetByteCount(serviceName);

            var result = Native.Iox2NativeMethods.iox2_service_name_new(
                IntPtr.Zero,
                serviceName,
                serviceNameBytes,
                out var serviceNameHandle);

            if (result != Native.Iox2NativeMethods.IOX2_OK)
                return Result<EventService, Iox2Error>.Err(Iox2Error.EventServiceCreationFailed);

            // Get service name ptr for builder
            var serviceNamePtr = Native.Iox2NativeMethods.iox2_cast_service_name_ptr(serviceNameHandle);

            // Create service builder
            var nodeHandle = _node._handle.DangerousGetHandle();
            var serviceBuilderHandle = Native.Iox2NativeMethods.iox2_node_service_builder(
                ref nodeHandle,
                IntPtr.Zero,
                serviceNamePtr);

            // Clean up service name
            Native.Iox2NativeMethods.iox2_service_name_drop(serviceNameHandle);

            if (serviceBuilderHandle == IntPtr.Zero)
                return Result<EventService, Iox2Error>.Err(Iox2Error.EventServiceCreationFailed);

            // Get event builder
            var eventBuilderHandle = Native.Iox2NativeMethods.iox2_service_builder_event(serviceBuilderHandle);

            if (eventBuilderHandle == IntPtr.Zero)
                return Result<EventService, Iox2Error>.Err(Iox2Error.EventServiceCreationFailed);

            // Create the event service
            var createResult = Native.Iox2NativeMethods.iox2_service_builder_event_create(
                eventBuilderHandle,
                IntPtr.Zero,
                out var portFactoryHandle);

            if (createResult != Native.Iox2NativeMethods.IOX2_OK)
                return Result<EventService, Iox2Error>.Err(Iox2Error.EventServiceCreationFailed);

            if (portFactoryHandle == IntPtr.Zero)
                return Result<EventService, Iox2Error>.Err(Iox2Error.EventServiceCreationFailed);

            var handle = new SafeEventServiceHandle(portFactoryHandle);
            var service = new EventService(handle);

            return Result<EventService, Iox2Error>.Ok(service);
        }
        catch (Exception)
        {
            return Result<EventService, Iox2Error>.Err(Iox2Error.EventServiceCreationFailed);
        }
    }
}