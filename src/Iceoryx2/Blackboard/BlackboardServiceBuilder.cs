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

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static Iceoryx2.Native.Iox2NativeMethods;

namespace Iceoryx2.Blackboard;

/// <summary>
/// Represents a key-value entry to be added to a blackboard service during creation.
/// </summary>
/// <typeparam name="TKey">The key type (must be unmanaged).</typeparam>
/// <typeparam name="TValue">The value type (must be unmanaged).</typeparam>
public sealed class BlackboardEntry<TKey, TValue>
    where TKey : unmanaged
    where TValue : unmanaged
{
    /// <summary>
    /// Gets the key for this entry.
    /// </summary>
    public TKey Key { get; }

    /// <summary>
    /// Gets the initial value for this entry.
    /// </summary>
    public TValue InitialValue { get; }

    /// <summary>
    /// Creates a new blackboard entry.
    /// </summary>
    /// <param name="key">The key for this entry.</param>
    /// <param name="initialValue">The initial value for this entry.</param>
    public BlackboardEntry(TKey key, TValue initialValue)
    {
        Key = key;
        InitialValue = initialValue;
    }
}

/// <summary>
/// Builder for creating or opening blackboard services.
/// </summary>
/// <typeparam name="TKey">The type of keys in the blackboard (must be unmanaged).</typeparam>
public sealed class BlackboardServiceBuilder<TKey>
    where TKey : unmanaged
{
    private readonly Node _node;
    private readonly Func<TKey, TKey, bool> _keyComparer;

    // Store the delegate to prevent garbage collection
    private iox2_service_blackboard_key_eq_cmp_func? _nativeKeyComparer;

    internal BlackboardServiceBuilder(Node node, Func<TKey, TKey, bool> keyComparer)
    {
        _node = node;
        _keyComparer = keyComparer;
    }

    /// <summary>
    /// Opens an existing blackboard service.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <returns>A Result containing the blackboard service or an error.</returns>
    public unsafe Result<BlackboardService<TKey>, Iox2Error> Open(string serviceName)
    {
        // Create service name
        var serviceNameResult = iox2_service_name_new(
            IntPtr.Zero,
            serviceName,
            serviceName.Length,
            out var serviceNameHandle);

        if (serviceNameResult != IOX2_OK)
        {
            return Result<BlackboardService<TKey>, Iox2Error>.Err(Iox2Error.BlackboardServiceCreationFailed);
        }

        try
        {
            var serviceNamePtr = iox2_cast_service_name_ptr(serviceNameHandle);

            // Get service builder
            var nodeHandle = _node._handle.DangerousGetHandle();
            var serviceBuilderHandle = iox2_node_service_builder(
                ref nodeHandle,
                IntPtr.Zero,
                serviceNamePtr);

            if (serviceBuilderHandle == IntPtr.Zero)
            {
                return Result<BlackboardService<TKey>, Iox2Error>.Err(Iox2Error.BlackboardServiceCreationFailed);
            }

            // Get blackboard opener builder
            var blackboardOpenerHandle = iox2_service_builder_blackboard_opener(serviceBuilderHandle);

            if (blackboardOpenerHandle == IntPtr.Zero)
            {
                return Result<BlackboardService<TKey>, Iox2Error>.Err(Iox2Error.BlackboardServiceCreationFailed);
            }

            // Set key type details
            var keyTypeName = ServiceBuilder.GetRustCompatibleTypeName<TKey>();
            var keyTypeSize = (ulong)sizeof(TKey);
            var keyTypeAlignment = GetAlignment<TKey>(keyTypeSize);

            var keyResult = iox2_service_builder_blackboard_opener_set_key_type_details(
                ref blackboardOpenerHandle,
                keyTypeName,
                keyTypeName.Length,
                keyTypeSize,
                keyTypeAlignment);

            if (keyResult != IOX2_OK)
            {
                return Result<BlackboardService<TKey>, Iox2Error>.Err(Iox2Error.BlackboardServiceCreationFailed);
            }

            // Open the service
            var result = iox2_service_builder_blackboard_open(
                blackboardOpenerHandle,
                IntPtr.Zero,
                out var portFactoryHandle);

            if (result != IOX2_OK)
            {
                return Result<BlackboardService<TKey>, Iox2Error>.Err(Iox2Error.BlackboardServiceCreationFailed);
            }

            return Result<BlackboardService<TKey>, Iox2Error>.Ok(
                new BlackboardService<TKey>(portFactoryHandle, _keyComparer));
        }
        finally
        {
            iox2_service_name_drop(serviceNameHandle);
        }
    }

    /// <summary>
    /// Creates a new blackboard service with the specified entries.
    /// </summary>
    /// <typeparam name="TValue">The value type for entries.</typeparam>
    /// <param name="serviceName">The name of the service.</param>
    /// <param name="entries">The key-value entries to add to the blackboard.</param>
    /// <returns>A Result containing the blackboard service or an error.</returns>
    public unsafe Result<BlackboardService<TKey>, Iox2Error> Create<TValue>(
        string serviceName,
        IEnumerable<BlackboardEntry<TKey, TValue>> entries)
        where TValue : unmanaged
    {
        // Create service name
        var serviceNameResult = iox2_service_name_new(
            IntPtr.Zero,
            serviceName,
            serviceName.Length,
            out var serviceNameHandle);

        if (serviceNameResult != IOX2_OK)
        {
            return Result<BlackboardService<TKey>, Iox2Error>.Err(Iox2Error.BlackboardServiceCreationFailed);
        }

        try
        {
            var serviceNamePtr = iox2_cast_service_name_ptr(serviceNameHandle);

            // Get service builder
            var nodeHandle = _node._handle.DangerousGetHandle();
            var serviceBuilderHandle = iox2_node_service_builder(
                ref nodeHandle,
                IntPtr.Zero,
                serviceNamePtr);

            if (serviceBuilderHandle == IntPtr.Zero)
            {
                return Result<BlackboardService<TKey>, Iox2Error>.Err(Iox2Error.BlackboardServiceCreationFailed);
            }

            // Get blackboard creator builder
            var blackboardCreatorHandle = iox2_service_builder_blackboard_creator(serviceBuilderHandle);

            if (blackboardCreatorHandle == IntPtr.Zero)
            {
                return Result<BlackboardService<TKey>, Iox2Error>.Err(Iox2Error.BlackboardServiceCreationFailed);
            }

            // Set key type details
            var keyTypeName = ServiceBuilder.GetRustCompatibleTypeName<TKey>();
            var keyTypeSize = (ulong)sizeof(TKey);
            var keyTypeAlignment = GetAlignment<TKey>(keyTypeSize);

            var keyResult = iox2_service_builder_blackboard_creator_set_key_type_details(
                ref blackboardCreatorHandle,
                keyTypeName,
                keyTypeName.Length,
                keyTypeSize,
                keyTypeAlignment);

            if (keyResult != IOX2_OK)
            {
                return Result<BlackboardService<TKey>, Iox2Error>.Err(Iox2Error.BlackboardServiceCreationFailed);
            }

            // Set key comparison function
            _nativeKeyComparer = CreateNativeKeyComparer();
            iox2_service_builder_blackboard_creator_set_key_eq_comparison_function(
                ref blackboardCreatorHandle,
                _nativeKeyComparer);

            // Add entries - keep memory alive until service is created
            var valueTypeName = ServiceBuilder.GetRustCompatibleTypeName<TValue>();
            var valueTypeSize = (ulong)sizeof(TValue);
            var valueTypeAlignment = GetAlignment<TValue>(valueTypeSize);

            // Collect all allocated memory to free after service creation
            var allocatedMemory = new List<IntPtr>();

            try
            {
                foreach (var entry in entries)
                {
                    // Allocate memory for key and value
                    var keyPtr = Marshal.AllocHGlobal(sizeof(TKey));
                    var valuePtr = Marshal.AllocHGlobal(sizeof(TValue));
                    allocatedMemory.Add(keyPtr);
                    allocatedMemory.Add(valuePtr);

                    // Copy key and value to unmanaged memory using unsafe pointer operations
                    // (Marshal.StructureToPtr doesn't work with enums and some value types)
                    var key = entry.Key;
                    var value = entry.InitialValue;
                    *(TKey*)keyPtr = key;
                    *(TValue*)valuePtr = value;

                    // Note: iox2_service_builder_blackboard_creator_add returns void
                    iox2_service_builder_blackboard_creator_add(
                        ref blackboardCreatorHandle,
                        keyPtr,
                        valuePtr,
                        null, // No release callback - we manage memory ourselves
                        valueTypeName,
                        valueTypeName.Length,
                        valueTypeSize,
                        valueTypeAlignment);
                }

                // Create the service
                var result = iox2_service_builder_blackboard_create(
                    blackboardCreatorHandle,
                    IntPtr.Zero,
                    out var portFactoryHandle);

                if (result != IOX2_OK)
                {
                    return Result<BlackboardService<TKey>, Iox2Error>.Err(Iox2Error.BlackboardServiceCreationFailed);
                }

                return Result<BlackboardService<TKey>, Iox2Error>.Ok(
                    new BlackboardService<TKey>(portFactoryHandle, _keyComparer));
            }
            finally
            {
                // Free all allocated memory after service creation
                foreach (var ptr in allocatedMemory)
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
        }
        finally
        {
            iox2_service_name_drop(serviceNameHandle);
        }
    }

    private unsafe iox2_service_blackboard_key_eq_cmp_func CreateNativeKeyComparer()
    {
        return (lhs, rhs) =>
        {
            // Use unsafe pointer dereference instead of Marshal.PtrToStructure
            // (Marshal.PtrToStructure doesn't work with enums and some value types)
            var leftKey = *(TKey*)lhs;
            var rightKey = *(TKey*)rhs;
            return _keyComparer(leftKey, rightKey);
        };
    }

    private static ulong GetAlignment<T>(ulong typeSize) where T : unmanaged
    {
        if (typeof(T).IsPrimitive)
        {
            return typeSize;
        }
        else
        {
            var layoutAttr = typeof(T).StructLayoutAttribute;
            if (layoutAttr != null && layoutAttr.Pack > 0)
            {
                return (ulong)layoutAttr.Pack;
            }
            else
            {
                return (ulong)IntPtr.Size;
            }
        }
    }
}