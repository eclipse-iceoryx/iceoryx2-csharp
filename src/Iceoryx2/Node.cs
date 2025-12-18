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

using Iceoryx2.Native;
using Iceoryx2.SafeHandles;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Iceoryx2;

/// <summary>
/// Represents a node in the Iceoryx2 system.
/// The node serves as a central entry point and is linked to a specific process within the Iceoryx2 ecosystem.
/// It provides capabilities for creating or opening services and managing node-specific resources.
/// </summary>
public sealed class Node : IDisposable
{
    internal SafeNodeHandle _handle;
    internal Iox2NativeMethods.iox2_service_type_e _serviceType;
    private bool _disposed;

    private static List<ServiceStaticConfig>? _tempServiceList;

    internal Node(SafeNodeHandle handle, Iox2NativeMethods.iox2_service_type_e serviceType = Iox2NativeMethods.iox2_service_type_e.IPC)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
        _serviceType = serviceType;
    }

    private static Iox2NativeMethods.iox2_callback_progression_e ServiceListCallbackStatic(IntPtr configPtr, IntPtr context)
    {
        try
        {
            if (configPtr == IntPtr.Zero || _tempServiceList == null)
            {
                return Iox2NativeMethods.iox2_callback_progression_e.STOP;
            }

            // Read the basic fields directly from memory without creating the full struct
            // to avoid issues with the union in iox2_static_config_t

            byte[] id = new byte[Iox2NativeMethods.IOX2_SERVICE_ID_LENGTH];
            byte[] name = new byte[Iox2NativeMethods.IOX2_SERVICE_NAME_LENGTH];

            // Read the id array
            Marshal.Copy(configPtr, id, 0, Iox2NativeMethods.IOX2_SERVICE_ID_LENGTH);

            // Read the name array (offset by id size)
            IntPtr namePtr = IntPtr.Add(configPtr, Iox2NativeMethods.IOX2_SERVICE_ID_LENGTH);
            Marshal.Copy(namePtr, name, 0, Iox2NativeMethods.IOX2_SERVICE_NAME_LENGTH);

            // Read the messaging pattern (offset by id + name)
            IntPtr patternPtr = IntPtr.Add(configPtr, Iox2NativeMethods.IOX2_SERVICE_ID_LENGTH + Iox2NativeMethods.IOX2_SERVICE_NAME_LENGTH);
            var messagingPattern = (Iox2NativeMethods.iox2_messaging_pattern_e)Marshal.ReadInt32(patternPtr);

            // Create a simplified service config without the problematic union
            var serviceConfig = new ServiceStaticConfig(id, name, messagingPattern);
            _tempServiceList.Add(serviceConfig);
            return Iox2NativeMethods.iox2_callback_progression_e.CONTINUE;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in service list callback: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return Iox2NativeMethods.iox2_callback_progression_e.STOP;
        }
    }

    /// <summary>
    /// Gets the name of the node.
    /// </summary>
    public string Name
    {
        get
        {
            ThrowIfDisposed();
            // TODO: Implement proper node name retrieval
            return "node"; // Placeholder
        }
    }

    /// <summary>
    /// Gets the unique ID of the node.
    /// </summary>
    public Guid Id
    {
        get
        {
            ThrowIfDisposed();
            // TODO: Implement proper node ID retrieval
            return Guid.NewGuid(); // Placeholder
        }
    }

    /// <summary>
    /// Creates a builder for creating or opening a service.
    /// </summary>
    public ServiceBuilder ServiceBuilder()
    {
        ThrowIfDisposed();
        return new ServiceBuilder(this);
    }

    /// <summary>
    /// Lists all available services in the system.
    /// </summary>
    /// <returns>A result containing a list of service static configurations or an error.</returns>
    public Result<List<ServiceStaticConfig>, ServiceListError> List()
    {
        ThrowIfDisposed();

        var services = new List<ServiceStaticConfig>();
        _tempServiceList = services;

        try
        {
            // Get the global config pointer
            var configPtr = Iox2NativeMethods.iox2_config_global_config();

            // Call the native service list function with static callback
            var result = Iox2NativeMethods.iox2_service_list(
                _serviceType,
                configPtr,  // Use global config instead of NULL
                ServiceListCallbackStatic,
                IntPtr.Zero   // No callback context
            );

            if (result != Iox2NativeMethods.IOX2_OK)
            {
                var errorCode = (Iox2NativeMethods.iox2_service_list_error_e)result;
                var error = errorCode switch
                {
                    Iox2NativeMethods.iox2_service_list_error_e.INSUFFICIENT_PERMISSIONS => ServiceListError.InsufficientPermissions,
                    Iox2NativeMethods.iox2_service_list_error_e.INTERNAL_ERROR => ServiceListError.InternalError,
                    Iox2NativeMethods.iox2_service_list_error_e.INTERRUPT => ServiceListError.Interrupt,
                    _ => ServiceListError.InternalError
                };
                return Result<List<ServiceStaticConfig>, ServiceListError>.Err(error);
            }

            return Result<List<ServiceStaticConfig>, ServiceListError>.Ok(services);
        }
        finally
        {
            _tempServiceList = null;
        }
    }

    /// <summary>
    /// Releases the unmanaged resources used by the Node and optionally releases the managed resources.
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
            throw new ObjectDisposedException(nameof(Node));
    }
}