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
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("Iceoryx2.Tests")]

namespace Iceoryx2.Native;

/// <summary>
/// Native P/Invoke methods for iceoryx2 C FFI.
/// Supports Linux, macOS, and Windows through dynamic library resolution.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static partial class Iox2NativeMethods
{
    private const string LibraryName = "iceoryx2_ffi_c";

    // ========================================
    // Cross-Platform Library Loading
    // ========================================

    static Iox2NativeMethods()
    {
        NativeLibrary.SetDllImportResolver(typeof(Iox2NativeMethods).Assembly, DllImportResolver);
    }

    private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != LibraryName)
            return IntPtr.Zero;

        // Try platform-specific library names
        string[] names;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            names = new[] { "iceoryx2_ffi_c.dll", "libiceoryx2_ffi_c.dll" };
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            names = new[] { "libiceoryx2_ffi_c.dylib", "iceoryx2_ffi_c.dylib" };
        else // Linux and Unix-like
            names = new[] { "libiceoryx2_ffi_c.so", "iceoryx2_ffi_c.so" };

        foreach (var name in names)
        {
            if (NativeLibrary.TryLoad(name, assembly, searchPath, out var handle))
                return handle;
        }

        return IntPtr.Zero;
    }

    // ========================================
    // Constants
    // ========================================

    internal const int IOX2_OK = 0;
    internal const int IOX2_NODE_NAME_LENGTH = 128;
    internal const int IOX2_SERVICE_NAME_LENGTH = 255;
    internal const int IOX2_SERVICE_ID_LENGTH = 32;

    // ========================================
    // Enums
    // ========================================

    internal enum iox2_service_type_e
    {
        LOCAL = 0,  // Must match C enum: LOCAL comes first
        IPC = 1     // Must match C enum: IPC comes second
    }

    internal enum iox2_log_level_e
    {
        TRACE = 0,
        DEBUG = 1,
        INFO = 2,
        WARN = 3,
        ERROR = 4,
        FATAL = 5
    }

    internal enum iox2_type_variant_e
    {
        FIXED_SIZE = 0,
        DYNAMIC = 1
    }

    internal enum iox2_messaging_pattern_e
    {
        PUBLISH_SUBSCRIBE = 0,
        EVENT = 1,
        REQUEST_RESPONSE = 2,
        BLACKBOARD = 3
    }

    internal enum iox2_service_list_error_e
    {
        INSUFFICIENT_PERMISSIONS = 1,
        INTERNAL_ERROR = 2,
        INTERRUPT = 3
    }

    // ========================================
    // Structs - Storage Types
    // ========================================

    // Node Builder Storage
    [StructLayout(LayoutKind.Sequential, Size = 18696, Pack = 8)]
    internal struct iox2_node_builder_storage_t { }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct iox2_node_builder_t
    {
        public iox2_node_builder_storage_t value;
        public IntPtr deleter;
    }

    // Node Storage
    [StructLayout(LayoutKind.Sequential, Size = 16, Pack = 8)]
    internal struct iox2_node_storage_t { }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct iox2_node_t
    {
        public iox2_service_type_e service_type;
        public iox2_node_storage_t value;
        public IntPtr deleter;
    }

    // Node Name Storage
    [StructLayout(LayoutKind.Sequential, Size = 152, Pack = 8)]
    internal struct iox2_node_name_storage_t { }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct iox2_node_name_t
    {
        public iox2_node_name_storage_t value;
        public IntPtr deleter;
    }

    // Service Name Storage
    [StructLayout(LayoutKind.Sequential, Size = 272, Pack = 8)]
    internal struct iox2_service_name_storage_t { }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct iox2_service_name_t
    {
        public iox2_service_name_storage_t value;
        public IntPtr deleter;
    }

    // Service Builder Storage
    [StructLayout(LayoutKind.Sequential, Size = 9104, Pack = 8)]
    internal struct iox2_service_builder_storage_t { }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct iox2_service_builder_t
    {
        public iox2_service_builder_storage_t value;
        public IntPtr deleter;
    }

    // Port Factory Pub/Sub Storage
    [StructLayout(LayoutKind.Sequential, Size = 1656, Pack = 8)]
    internal struct iox2_port_factory_pub_sub_storage_t { }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct iox2_port_factory_pub_sub_t
    {
        public iox2_service_type_e service_type;
        public iox2_port_factory_pub_sub_storage_t value;
        public IntPtr deleter;
    }

    // Publisher Builder Storage
    [StructLayout(LayoutKind.Sequential, Size = 128, Pack = 16)]
    internal struct iox2_port_factory_publisher_builder_storage_t { }

    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    internal struct iox2_port_factory_publisher_builder_t
    {
        public iox2_service_type_e service_type;
        public iox2_port_factory_publisher_builder_storage_t value;
        public IntPtr deleter;
    }

    // Subscriber Builder Storage
    [StructLayout(LayoutKind.Sequential, Size = 112, Pack = 16)]
    internal struct iox2_port_factory_subscriber_builder_storage_t { }

    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    internal struct iox2_port_factory_subscriber_builder_t
    {
        public iox2_service_type_e service_type;
        public iox2_port_factory_subscriber_builder_storage_t value;
        public IntPtr deleter;
    }

    // Publisher Storage
    [StructLayout(LayoutKind.Sequential, Size = 248, Pack = 16)]
    internal struct iox2_publisher_storage_t { }

    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    internal struct iox2_publisher_t
    {
        public iox2_service_type_e service_type;
        public iox2_publisher_storage_t value;
        public IntPtr deleter;
    }

    // Subscriber Storage
    [StructLayout(LayoutKind.Sequential, Size = 1232, Pack = 16)]
    internal struct iox2_subscriber_storage_t { }

    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    internal struct iox2_subscriber_t
    {
        public iox2_service_type_e service_type;
        public iox2_subscriber_storage_t value;
        public IntPtr deleter;
    }

    // Sample Mut Storage
    [StructLayout(LayoutKind.Sequential, Size = 64, Pack = 8)]
    internal struct iox2_sample_mut_storage_t { }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct iox2_sample_mut_t
    {
        public iox2_service_type_e service_type;
        public iox2_sample_mut_storage_t value;
        public IntPtr deleter;
    }

    // Sample Storage
    [StructLayout(LayoutKind.Sequential, Size = 96, Pack = 16)]
    internal struct iox2_sample_storage_t { }

    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    internal struct iox2_sample_t
    {
        public iox2_service_type_e service_type;
        public iox2_sample_storage_t value;
        public IntPtr deleter;
    }

    // Port Factory Event Storage
    [StructLayout(LayoutKind.Sequential, Size = 1656, Pack = 8)]
    internal struct iox2_port_factory_event_storage_t { }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct iox2_port_factory_event_t
    {
        public iox2_service_type_e service_type;
        public iox2_port_factory_event_storage_t value;
        public IntPtr deleter;
    }

    // Notifier Builder Storage
    [StructLayout(LayoutKind.Sequential, Size = 24, Pack = 8)]
    internal struct iox2_port_factory_notifier_builder_storage_t { }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct iox2_port_factory_notifier_builder_t
    {
        public iox2_service_type_e service_type;
        public iox2_port_factory_notifier_builder_storage_t value;
        public IntPtr deleter;
    }

    // Listener Builder Storage
    [StructLayout(LayoutKind.Sequential, Size = 24, Pack = 8)]
    internal struct iox2_port_factory_listener_builder_storage_t { }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct iox2_port_factory_listener_builder_t
    {
        public iox2_service_type_e service_type;
        public iox2_port_factory_listener_builder_storage_t value;
        public IntPtr deleter;
    }

    // Notifier Storage
    [StructLayout(LayoutKind.Sequential, Size = 1656, Pack = 8)]
    internal struct iox2_notifier_storage_t { }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct iox2_notifier_t
    {
        public iox2_service_type_e service_type;
        public iox2_notifier_storage_t value;
        public IntPtr deleter;
    }

    // Listener Storage
    [StructLayout(LayoutKind.Sequential, Size = 1656, Pack = 8)]
    internal struct iox2_listener_storage_t { }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct iox2_listener_t
    {
        public iox2_service_type_e service_type;
        public iox2_listener_storage_t value;
        public IntPtr deleter;
    }

    // Event ID
    [StructLayout(LayoutKind.Sequential)]
    internal struct iox2_event_id_t
    {
        public UIntPtr value;
    }

    // Port Factory Request Response Storage
    [StructLayout(LayoutKind.Sequential, Size = 1656, Pack = 8)]
    internal struct iox2_port_factory_request_response_storage_t { }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct iox2_port_factory_request_response_t
    {
        public iox2_service_type_e service_type;
        public iox2_port_factory_request_response_storage_t value;
        public IntPtr deleter;
    }

    // Client Builder Storage
    [StructLayout(LayoutKind.Sequential, Size = 24, Pack = 8)]
    internal struct iox2_port_factory_client_builder_storage_t { }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct iox2_port_factory_client_builder_t
    {
        public iox2_service_type_e service_type;
        public iox2_port_factory_client_builder_storage_t value;
        public IntPtr deleter;
    }

    // Server Builder Storage
    [StructLayout(LayoutKind.Sequential, Size = 24, Pack = 8)]
    internal struct iox2_port_factory_server_builder_storage_t { }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct iox2_port_factory_server_builder_t
    {
        public iox2_service_type_e service_type;
        public iox2_port_factory_server_builder_storage_t value;
        public IntPtr deleter;
    }

    // Client Storage
    [StructLayout(LayoutKind.Sequential, Size = 248, Pack = 16)]
    internal struct iox2_client_storage_t { }

    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    internal struct iox2_client_t
    {
        public iox2_service_type_e service_type;
        public iox2_client_storage_t value;
        public IntPtr deleter;
    }

    // Server Storage
    [StructLayout(LayoutKind.Sequential, Size = 1248, Pack = 16)]
    internal struct iox2_server_storage_t { }

    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    internal struct iox2_server_t
    {
        public iox2_service_type_e service_type;
        public iox2_server_storage_t value;
        public IntPtr deleter;
    }

    // Request Mut Storage
    [StructLayout(LayoutKind.Sequential, Size = 64, Pack = 8)]
    internal struct iox2_request_mut_storage_t { }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct iox2_request_mut_t
    {
        public iox2_service_type_e service_type;
        public iox2_request_mut_storage_t value;
        public IntPtr deleter;
    }

    // Request Storage
    [StructLayout(LayoutKind.Sequential, Size = 96, Pack = 16)]
    internal struct iox2_request_storage_t { }

    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    internal struct iox2_request_t
    {
        public iox2_service_type_e service_type;
        public iox2_request_storage_t value;
        public IntPtr deleter;
    }

    // Response Mut Storage
    [StructLayout(LayoutKind.Sequential, Size = 64, Pack = 8)]
    internal struct iox2_response_mut_storage_t { }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct iox2_response_mut_t
    {
        public iox2_service_type_e service_type;
        public iox2_response_mut_storage_t value;
        public IntPtr deleter;
    }

    // Response Storage
    [StructLayout(LayoutKind.Sequential, Size = 96, Pack = 16)]
    internal struct iox2_response_storage_t { }

    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    internal struct iox2_response_t
    {
        public iox2_service_type_e service_type;
        public iox2_response_storage_t value;
        public IntPtr deleter;
    }

    // Pending Response Storage
    [StructLayout(LayoutKind.Sequential, Size = 32, Pack = 8)]
    internal struct iox2_pending_response_storage_t { }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct iox2_pending_response_t
    {
        public iox2_service_type_e service_type;
        public iox2_pending_response_storage_t value;
        public IntPtr deleter;
    }

    // ========================================
    // Logging API
    // ========================================

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_set_log_level_from_env_or(iox2_log_level_e log_level);

    // ========================================
    // Node Builder API
    // ========================================

    /// <summary>
    /// Creates a new node builder.
    /// C signature: iox2_node_builder_h iox2_node_builder_new(struct iox2_node_builder_t *node_builder_struct_ptr)
    /// Returns: handle to the builder (pointer to opaque type)
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr iox2_node_builder_new(ref iox2_node_builder_t node_builder_struct);

    /// <summary>
    /// Sets the name for the node builder.
    /// C signature: void iox2_node_builder_set_name(iox2_node_builder_h_ref node_builder_handle, iox2_node_name_ptr node_name_ptr)
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_node_builder_set_name(
        ref IntPtr node_builder_handle,  // Pass by reference - C expects pointer to handle
        IntPtr node_name_ptr);

    /// <summary>
    /// Creates a node from the builder.
    /// C signature: int iox2_node_builder_create(iox2_node_builder_h node_builder_handle,
    ///                                           struct iox2_node_t *node_struct_ptr,
    ///                                           enum iox2_service_type_e service_type,
    ///                                           iox2_node_h *node_handle_ptr)
    /// Returns: IOX2_OK (0) on success, error code otherwise
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_node_builder_create(
        IntPtr node_builder_handle,
        IntPtr node_struct_ptr,  // Changed to IntPtr to allow passing NULL
        iox2_service_type_e service_type,
        out IntPtr node_handle);

    // ========================================
    // Service Discovery - Type Details
    // ========================================

    [StructLayout(LayoutKind.Sequential)]
    internal struct iox2_type_detail_t
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] type_name;
        public int type_name_len;
        public ulong size;
        public ulong alignment;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct iox2_message_type_details_t
    {
        public iox2_type_detail_t header;
        public iox2_type_detail_t user_header;
        public iox2_type_detail_t payload;
    }

    // ========================================
    // Service Discovery - Static Config Structs
    // ========================================

    [StructLayout(LayoutKind.Sequential)]
    internal struct iox2_static_config_event_t
    {
        public UIntPtr max_notifiers;
        public UIntPtr max_listeners;
        public UIntPtr max_nodes;
        public UIntPtr event_id_max_value;
        public UIntPtr notifier_dead_event;
        [MarshalAs(UnmanagedType.U1)]
        public bool has_notifier_dead_event;
        public UIntPtr notifier_dropped_event;
        [MarshalAs(UnmanagedType.U1)]
        public bool has_notifier_dropped_event;
        public UIntPtr notifier_created_event;
        [MarshalAs(UnmanagedType.U1)]
        public bool has_notifier_created_event;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct iox2_static_config_publish_subscribe_t
    {
        public UIntPtr max_subscribers;
        public UIntPtr max_publishers;
        public UIntPtr max_nodes;
        public UIntPtr history_size;
        public UIntPtr subscriber_max_buffer_size;
        public UIntPtr subscriber_max_borrowed_samples;
        [MarshalAs(UnmanagedType.U1)]
        public bool enable_safe_overflow;
        public iox2_message_type_details_t message_type_details;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct iox2_static_config_request_response_t
    {
        [MarshalAs(UnmanagedType.U1)]
        public bool enable_safe_overflow_for_requests;
        [MarshalAs(UnmanagedType.U1)]
        public bool enable_safe_overflow_for_responses;
        [MarshalAs(UnmanagedType.U1)]
        public bool enable_fire_and_forget_requests;
        public UIntPtr max_active_requests_per_client;
        public UIntPtr max_loaned_requests;
        public UIntPtr max_response_buffer_size;
        public UIntPtr max_servers;
        public UIntPtr max_clients;
        public UIntPtr max_nodes;
        public UIntPtr max_borrowed_responses_per_pending_response;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct iox2_static_config_blackboard_t
    {
        public UIntPtr max_readers;
        public UIntPtr max_writers;
        public UIntPtr max_nodes;
        public iox2_type_detail_t type_details;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct iox2_static_config_details_t
    {
        [FieldOffset(0)]
        public iox2_static_config_event_t @event;
        [FieldOffset(0)]
        public iox2_static_config_publish_subscribe_t publish_subscribe;
        [FieldOffset(0)]
        public iox2_static_config_request_response_t request_response;
        [FieldOffset(0)]
        public iox2_static_config_blackboard_t blackboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct iox2_static_config_t
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = IOX2_SERVICE_ID_LENGTH)]
        public byte[] id;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = IOX2_SERVICE_NAME_LENGTH)]
        public byte[] name;
        public iox2_messaging_pattern_e messaging_pattern;
        public iox2_static_config_details_t details;
        public IntPtr attributes;  // iox2_attribute_set_h_ref
    }

    // ========================================
    // Service Discovery - Callback Delegate
    // ========================================

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate iox2_callback_progression_e iox2_service_list_callback(
        IntPtr static_config_ptr,
        IntPtr callback_context);

    // ========================================
    // Node API
    // ========================================

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_node_drop(IntPtr node_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr iox2_node_service_builder(
        ref IntPtr node_handle,  // Pass by reference - C expects pointer to handle
        IntPtr service_builder_struct_ptr,  // Changed to IntPtr to allow passing NULL
        IntPtr service_name_ptr);

    // ========================================
    // Service Discovery API
    // ========================================

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_service_list(
        iox2_service_type_e service_type,
        IntPtr config_ptr,  // iox2_config_ptr - can be null
        iox2_service_list_callback callback,
        IntPtr callback_context);

    // ========================================
    // Node Name API
    // ========================================

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_node_name_new(
        IntPtr node_name_struct,  // Changed to IntPtr to allow passing NULL
        [MarshalAs(UnmanagedType.LPUTF8Str)] string node_name_str,
        int node_name_len,
        out IntPtr node_name_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_node_name_drop(IntPtr node_name_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr iox2_cast_node_name_ptr(IntPtr node_name_handle);

    // ========================================
    // Service Name API
    // ========================================

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_service_name_new(
        IntPtr service_name_struct,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string service_name_str,
        int service_name_len,
        out IntPtr service_name_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_service_name_drop(IntPtr service_name_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr iox2_cast_service_name_ptr(IntPtr service_name_handle);

    // ========================================
    // Service Builder Pub/Sub API
    // ========================================

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr iox2_service_builder_pub_sub(IntPtr service_builder_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_service_builder_pub_sub_set_payload_type_details(
        ref IntPtr service_builder_pub_sub_handle,  // Pass by reference - C expects pointer to handle
        iox2_type_variant_e type_variant,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string type_name,
        int type_name_len,
        ulong type_size,
        ulong type_alignment);

    // QoS Settings for Publish-Subscribe Service Builder
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_service_builder_pub_sub_set_max_subscribers(
        ref IntPtr service_builder_pub_sub_handle,
        UIntPtr value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_service_builder_pub_sub_set_max_publishers(
        ref IntPtr service_builder_pub_sub_handle,
        UIntPtr value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_service_builder_pub_sub_set_subscriber_max_buffer_size(
        ref IntPtr service_builder_pub_sub_handle,
        UIntPtr value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_service_builder_pub_sub_set_subscriber_max_borrowed_samples(
        ref IntPtr service_builder_pub_sub_handle,
        UIntPtr value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_service_builder_pub_sub_set_history_size(
        ref IntPtr service_builder_pub_sub_handle,
        UIntPtr value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_service_builder_pub_sub_set_enable_safe_overflow(
        ref IntPtr service_builder_pub_sub_handle,
        [MarshalAs(UnmanagedType.I1)] bool value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_service_builder_pub_sub_open_or_create(
        IntPtr service_builder_pub_sub_handle,
        IntPtr port_factory_struct_ptr,  // Changed to IntPtr to allow passing NULL
        out IntPtr port_factory_handle);

    // ========================================
    // Port Factory Pub/Sub API
    // ========================================

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_port_factory_pub_sub_drop(IntPtr port_factory_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr iox2_port_factory_pub_sub_publisher_builder(
        ref IntPtr port_factory_handle,  // Pass by reference - C expects pointer to handle
        IntPtr publisher_builder_struct_ptr);  // Changed to IntPtr to allow passing NULL

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr iox2_port_factory_pub_sub_subscriber_builder(
        ref IntPtr port_factory_handle,  // Pass by reference - C expects pointer to handle
        IntPtr subscriber_builder_struct_ptr);  // Changed to IntPtr to allow passing NULL

    // ========================================
    // Publisher API
    // ========================================

    // QoS Settings for Publisher Builder
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_port_factory_publisher_builder_set_max_loaned_samples(
        ref IntPtr publisher_builder_handle,
        UIntPtr value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_port_factory_publisher_builder_create(
        IntPtr publisher_builder_handle,
        IntPtr publisher_struct_ptr,  // Changed to IntPtr to allow passing NULL
        out IntPtr publisher_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_publisher_drop(IntPtr publisher_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_publisher_update_connections(ref IntPtr publisher_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_publisher_loan_slice_uninit(
        ref IntPtr publisher_handle,  // Pass by reference - C expects pointer to handle
        IntPtr sample_struct_ptr,  // Changed to IntPtr to allow passing NULL
        out IntPtr sample_handle,
        UIntPtr number_of_elements);  // size_t in C = UIntPtr in C# (8 bytes on 64-bit)

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_sample_mut_send(
        IntPtr sample_handle,
        IntPtr send_error_struct_ptr);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_publisher_send_copy(
        ref IntPtr publisher_handle,
        IntPtr data_ptr,
        UIntPtr data_len,
        IntPtr number_of_recipients);

    // ========================================
    // Subscriber API
    // ========================================

    // Subscriber Builder QoS
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_port_factory_subscriber_builder_set_buffer_size(
        ref IntPtr subscriber_builder_handle,
        UIntPtr value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_port_factory_subscriber_builder_create(
        IntPtr subscriber_builder_handle,
        IntPtr subscriber_struct_ptr,  // Changed to IntPtr to allow passing NULL
        out IntPtr subscriber_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_subscriber_drop(IntPtr subscriber_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_subscriber_receive(
        ref IntPtr subscriber_handle,  // Pass by reference - C expects pointer to handle
        IntPtr sample_struct_ptr,  // Changed to IntPtr to allow passing NULL
        out IntPtr sample_handle);

    // ========================================
    // Sample API
    // ========================================

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_sample_drop(IntPtr sample_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_sample_payload(
        ref IntPtr sample_handle,  // Non-owning reference (_ref type) - needs ref to pass pointer-to-pointer
        out IntPtr payload_ptr,
        out UIntPtr payload_len);  // c_size_t in C = UIntPtr in C#

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_sample_mut_payload_mut(
        ref IntPtr sample_handle,  // Non-owning reference (_ref type) - needs ref to pass pointer-to-pointer
        out IntPtr payload_ptr,
        out UIntPtr payload_len);  // c_size_t in C = UIntPtr in C#

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "iox2_sample_mut_payload_mut")]
    internal static extern void iox2_sample_mut_payload_mut_ptr(
        ref IntPtr sample_handle,
        out IntPtr payload_ptr,
        IntPtr payload_len_or_null);  // Can pass IntPtr.Zero for NULL

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_sample_mut_drop(IntPtr sample_handle);

    // ========================================
    // Service Builder Event API
    // ========================================

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr iox2_service_builder_event(IntPtr service_builder_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_service_builder_event_open_or_create(
        IntPtr service_builder_event_handle,
        IntPtr port_factory_struct_ptr,  // Changed to IntPtr to allow passing NULL
        out IntPtr port_factory_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_service_builder_event_open(
        IntPtr service_builder_event_handle,
        IntPtr port_factory_struct_ptr,
        out IntPtr port_factory_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_service_builder_event_create(
        IntPtr service_builder_event_handle,
        IntPtr port_factory_struct_ptr,
        out IntPtr port_factory_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_service_builder_event_set_max_notifiers(
        ref IntPtr service_builder_handle,
        UIntPtr value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_service_builder_event_set_max_listeners(
        ref IntPtr service_builder_handle,
        UIntPtr value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_service_builder_event_set_max_nodes(
        ref IntPtr service_builder_handle,
        UIntPtr value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_service_builder_event_set_event_id_max_value(
        ref IntPtr service_builder_handle,
        UIntPtr value);

    // ========================================
    // Port Factory Event API
    // ========================================

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_port_factory_event_drop(IntPtr port_factory_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr iox2_port_factory_event_notifier_builder(
        ref IntPtr port_factory_handle,  // Pass by reference - C expects pointer to handle
        IntPtr notifier_builder_struct_ptr);  // Changed to IntPtr to allow passing NULL

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr iox2_port_factory_event_listener_builder(
        ref IntPtr port_factory_handle,  // Pass by reference - C expects pointer to handle
        IntPtr listener_builder_struct_ptr);  // Changed to IntPtr to allow passing NULL

    // ========================================
    // Notifier Builder API
    // ========================================

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_port_factory_notifier_builder_set_default_event_id(
        ref IntPtr notifier_builder_handle,
        ref iox2_event_id_t event_id);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_port_factory_notifier_builder_create(
        IntPtr notifier_builder_handle,
        IntPtr notifier_struct_ptr,  // Changed to IntPtr to allow passing NULL
        out IntPtr notifier_handle);

    // ========================================
    // Notifier API
    // ========================================

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_notifier_drop(IntPtr notifier_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_notifier_notify(
        ref IntPtr notifier_handle,  // Pass by reference - C expects pointer to handle
        IntPtr number_of_notified_listeners_ptr);  // Can be NULL

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_notifier_notify_with_custom_event_id(
        ref IntPtr notifier_handle,
        ref iox2_event_id_t custom_event_id,
        IntPtr number_of_notified_listeners_ptr);  // Can be NULL

    // ========================================
    // Listener Builder API
    // ========================================

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_port_factory_listener_builder_create(
        IntPtr listener_builder_handle,
        IntPtr listener_struct_ptr,  // Changed to IntPtr to allow passing NULL
        out IntPtr listener_handle);

    // ========================================
    // Listener API
    // ========================================

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_listener_drop(IntPtr listener_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_listener_try_wait_one(
        ref IntPtr listener_handle,
        out iox2_event_id_t event_id,
        out bool has_received_one);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_listener_timed_wait_one(
        ref IntPtr listener_handle,
        out iox2_event_id_t event_id,
        out bool has_received_one,
        ulong seconds,
        uint nanoseconds);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_listener_blocking_wait_one(
        ref IntPtr listener_handle,
        out iox2_event_id_t event_id,
        out bool has_received_one);

    // ========================================
    // Service Builder Request Response API
    // ========================================

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr iox2_service_builder_request_response(IntPtr service_builder_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_service_builder_request_response_set_request_payload_type_details(
        ref IntPtr service_builder_handle,
        iox2_type_variant_e type_variant,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string type_name,
        int type_name_len,
        ulong type_size,
        ulong type_alignment);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_service_builder_request_response_set_response_payload_type_details(
        ref IntPtr service_builder_handle,
        iox2_type_variant_e type_variant,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string type_name,
        int type_name_len,
        ulong type_size,
        ulong type_alignment);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_service_builder_request_response_open_or_create(
        IntPtr service_builder_handle,
        IntPtr port_factory_struct_ptr,
        out IntPtr port_factory_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_service_builder_request_response_open(
        IntPtr service_builder_handle,
        IntPtr port_factory_struct_ptr,
        out IntPtr port_factory_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_service_builder_request_response_create(
        IntPtr service_builder_handle,
        IntPtr port_factory_struct_ptr,
        out IntPtr port_factory_handle);

    // ========================================
    // Port Factory Request Response API
    // ========================================

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_port_factory_request_response_drop(IntPtr port_factory_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr iox2_port_factory_request_response_client_builder(
        ref IntPtr port_factory_handle,
        IntPtr client_builder_struct_ptr);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr iox2_port_factory_request_response_server_builder(
        ref IntPtr port_factory_handle,
        IntPtr server_builder_struct_ptr);

    // ========================================
    // Client Builder API
    // ========================================

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_port_factory_client_builder_create(
        IntPtr client_builder_handle,
        IntPtr client_struct_ptr,
        out IntPtr client_handle);

    // ========================================
    // Client API
    // ========================================

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_client_drop(IntPtr client_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_client_loan_slice_uninit(
        ref IntPtr client_handle,
        IntPtr request_struct_ptr,
        out IntPtr request_handle,
        UIntPtr number_of_elements);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_request_mut_send(
        IntPtr request_handle,
        IntPtr pending_response_struct_ptr,
        out IntPtr pending_response_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_client_send_copy(
        ref IntPtr client_handle,
        IntPtr data_ptr,
        UIntPtr size_of_element,
        UIntPtr number_of_elements,
        IntPtr pending_response_struct_ptr,
        out IntPtr pending_response_handle);

    // ========================================
    // Server Builder API
    // ========================================

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_port_factory_server_builder_create(
        IntPtr server_builder_handle,
        IntPtr server_struct_ptr,
        out IntPtr server_handle);

    // ========================================
    // Server API
    // ========================================

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_server_drop(IntPtr server_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_server_receive(
        ref IntPtr server_handle,
        IntPtr active_request_struct_ptr,
        out IntPtr active_request_handle);

    // ========================================
    // ActiveRequest API (server-side request)
    // ========================================

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_active_request_drop(IntPtr active_request_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_active_request_payload(
        ref IntPtr active_request_handle,
        out IntPtr payload_ptr,
        out UIntPtr payload_len);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_active_request_loan_slice_uninit(
        ref IntPtr active_request_handle,
        IntPtr response_struct_ptr,
        out IntPtr response_handle,
        UIntPtr number_of_elements);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_active_request_send_copy(
        ref IntPtr active_request_handle,
        IntPtr data_ptr,
        UIntPtr data_len,
        UIntPtr number_of_elements);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_request_mut_payload_mut(
        ref IntPtr request_handle,
        out IntPtr payload_ptr,
        out UIntPtr payload_len);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_request_mut_drop(IntPtr request_handle);

    // ========================================
    // Response API
    // ========================================

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_response_drop(IntPtr response_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_response_payload(
        ref IntPtr response_handle,
        out IntPtr payload_ptr,
        out UIntPtr payload_len);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_response_mut_payload_mut(
        ref IntPtr response_handle,
        out IntPtr payload_ptr,
        out UIntPtr payload_len);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_response_mut_send(IntPtr response_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_response_mut_drop(IntPtr response_handle);

    // ========================================
    // Pending Response API
    // ========================================

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_pending_response_drop(IntPtr pending_response_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_pending_response_receive(
        ref IntPtr pending_response_handle,
        IntPtr response_struct_ptr,
        out IntPtr response_handle);

    // ========================================
    // Config API
    // ========================================

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr iox2_config_global_config();

    // ========================================
    // Additional Logging API
    // ========================================

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void iox2_log_callback(iox2_log_level_e log_level, IntPtr origin, IntPtr message);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_log(iox2_log_level_e log_level, IntPtr origin, IntPtr message);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static extern bool iox2_use_console_logger();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static extern bool iox2_use_file_logger(IntPtr log_file);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_set_log_level_from_env_or_default();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_set_log_level(iox2_log_level_e level);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern iox2_log_level_e iox2_get_log_level();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static extern bool iox2_set_logger(iox2_log_callback logger);

    // ========================================
    // WaitSet API - Enums and Delegates
    // ========================================

    internal enum iox2_signal_handling_mode_e
    {
        DISABLED = 0,
        TERMINATION = 1,
        INTERRUPT = 2,
        TERMINATION_AND_INTERRUPT = 3
    }

    internal enum iox2_callback_progression_e
    {
        STOP = 0,
        CONTINUE = 1
    }

    internal enum iox2_waitset_run_result_e
    {
        TERMINATION_REQUEST = IOX2_OK + 1,
        INTERRUPT = IOX2_OK + 2,
        STOP_REQUEST = IOX2_OK + 3,
        ALL_EVENTS_HANDLED = IOX2_OK + 4
    }

    internal enum iox2_waitset_run_error_e
    {
        INSUFFICIENT_PERMISSIONS = IOX2_OK + 1,
        INTERNAL_ERROR = IOX2_OK + 2,
        NO_ATTACHMENTS = IOX2_OK + 3,
        TERMINATION_REQUEST = IOX2_OK + 4,
        INTERRUPT = IOX2_OK + 5
    }

    internal enum iox2_waitset_attachment_error_e
    {
        INSUFFICIENT_CAPACITY = IOX2_OK + 1,
        ALREADY_ATTACHED = IOX2_OK + 2,
        INTERNAL_ERROR = IOX2_OK + 3,
        INSUFFICIENT_RESOURCES = IOX2_OK + 4
    }

    internal enum iox2_waitset_create_error_e
    {
        INTERNAL_ERROR = IOX2_OK + 1,
        INSUFFICIENT_RESOURCES = IOX2_OK + 2
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate iox2_callback_progression_e iox2_waitset_run_callback(
        IntPtr attachment_id_handle,
        IntPtr callback_context);

    // ========================================
    // WaitSetBuilder API
    // ========================================

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_waitset_builder_new(
        IntPtr struct_ptr,
        out IntPtr handle_ptr);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_waitset_builder_drop(IntPtr handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_waitset_builder_create(
        IntPtr builder_handle,
        iox2_service_type_e service_type,
        IntPtr struct_ptr,
        out IntPtr waitset_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_waitset_builder_set_signal_handling_mode(
        ref IntPtr builder_handle_ref,
        iox2_signal_handling_mode_e mode);

    // ========================================
    // WaitSet API
    // ========================================

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_waitset_drop(IntPtr handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static extern bool iox2_waitset_is_empty(ref IntPtr handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern UIntPtr iox2_waitset_len(ref IntPtr handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern UIntPtr iox2_waitset_capacity(ref IntPtr handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern iox2_signal_handling_mode_e iox2_waitset_signal_handling_mode(ref IntPtr handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_waitset_attach_notification(
        ref IntPtr waitset_handle,
        IntPtr file_descriptor,
        IntPtr guard_struct_ptr,
        out IntPtr guard_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_waitset_attach_deadline(
        ref IntPtr waitset_handle,
        IntPtr file_descriptor,
        ulong seconds,
        uint nanoseconds,
        IntPtr guard_struct_ptr,
        out IntPtr guard_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_waitset_attach_interval(
        ref IntPtr waitset_handle,
        ulong seconds,
        uint nanoseconds,
        IntPtr guard_struct_ptr,
        out IntPtr guard_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_waitset_wait_and_process(
        ref IntPtr waitset_handle,
        iox2_waitset_run_callback callback,
        IntPtr callback_context,
        out iox2_waitset_run_result_e result);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_waitset_wait_and_process_once(
        ref IntPtr waitset_handle,
        iox2_waitset_run_callback callback,
        IntPtr callback_context,
        out iox2_waitset_run_result_e result);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int iox2_waitset_wait_and_process_once_with_timeout(
        ref IntPtr waitset_handle,
        iox2_waitset_run_callback callback,
        IntPtr callback_context,
        ulong seconds,
        uint nanoseconds,
        out iox2_waitset_run_result_e result);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_waitset_stop(ref IntPtr waitset_handle);

    // ========================================
    // WaitSetGuard API
    // ========================================

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_waitset_guard_drop(IntPtr handle);

    // ========================================
    // WaitSetAttachmentId API
    // ========================================

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void iox2_waitset_attachment_id_drop(IntPtr handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static extern bool iox2_waitset_attachment_id_equal(
        ref IntPtr lhs,
        ref IntPtr rhs);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static extern bool iox2_waitset_attachment_id_less(
        ref IntPtr lhs,
        ref IntPtr rhs);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static extern bool iox2_waitset_attachment_id_has_event_from(
        ref IntPtr attachment_id_handle,
        ref IntPtr guard_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static extern bool iox2_waitset_attachment_id_has_missed_deadline(
        ref IntPtr attachment_id_handle,
        ref IntPtr guard_handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr iox2_waitset_create_error_string(iox2_waitset_create_error_e error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr iox2_waitset_attachment_error_string(iox2_waitset_attachment_error_e error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr iox2_waitset_run_error_string(iox2_waitset_run_error_e error);

    // ========================================
    // FileDescriptor API (needed for WaitSet attachments)
    // ========================================

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr iox2_listener_get_file_descriptor(ref IntPtr listener_handle);
}