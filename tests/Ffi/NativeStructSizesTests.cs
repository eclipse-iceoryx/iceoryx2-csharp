// Copyright (c) 2026 Contributors to the Eclipse Foundation
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
using System.Runtime.InteropServices;
using Xunit;

namespace Iceoryx2.Tests.Ffi;

/// <summary>
/// Tripwire tests for P/Invoke struct sizes.
///
/// Source of truth: iceoryx2/target/release/iceoryx2-ffi-c-cbindgen/include/iox2/iceoryx2.h
/// Each constant below mirrors a `uint8_t internal[N]` storage-struct size in
/// that header. If the Rust FFI changes a storage size, the header regenerates,
/// the bindings must be updated, and both the Size attribute in
/// Iox2NativeMethods.cs and the constant below must be updated together.
/// </summary>
public class NativeStructSizesTests
{
    // INTENTIONAL DUPLICATION of the Size attribute in Iox2NativeMethods.cs.
    // Sharing a single const between the StructLayout Size and the test would
    // make the assert trivially pass (both sides derive from the same value)
    // and erase the tripwire. The point of the duplication is double-entry
    // bookkeeping against the C header: if a maintainer updates one side but
    // forgets the other, this test fails.
    private const int NodeBuilderStorageSize = 18696;        // iox2_node_builder_storage_t
    private const int NodeStorageSize = 16;                  // iox2_node_storage_t
    private const int NodeNameStorageSize = 152;             // iox2_node_name_storage_t
    private const int ServiceNameStorageSize = 272;          // iox2_service_name_storage_t
    private const int ServiceBuilderStorageSize = 9104;      // iox2_service_builder_storage_t
    private const int PortFactoryPubSubStorageSize = 1656;   // iox2_port_factory_pub_sub_storage_t
    private const int PublisherBuilderStorageSize = 208;     // iox2_port_factory_publisher_builder_storage_t
    private const int SubscriberBuilderStorageSize = 112;    // iox2_port_factory_subscriber_builder_storage_t
    private const int PublisherStorageSize = 248;            // iox2_publisher_storage_t
    private const int SubscriberStorageSize = 1232;          // iox2_subscriber_storage_t
    private const int SampleMutStorageSize = 64;             // iox2_sample_mut_storage_t
    private const int SampleStorageSize = 96;                // iox2_sample_storage_t
    private const int PortFactoryEventStorageSize = 1656;    // iox2_port_factory_event_storage_t
    private const int NotifierBuilderStorageSize = 24;       // iox2_port_factory_notifier_builder_storage_t
    private const int ListenerBuilderStorageSize = 24;       // iox2_port_factory_listener_builder_storage_t
    private const int NotifierStorageSize = 1656;            // iox2_notifier_storage_t
    private const int ListenerStorageSize = 1656;            // iox2_listener_storage_t
    private const int PortFactoryReqRespStorageSize = 1656;  // iox2_port_factory_request_response_storage_t
    private const int ClientBuilderStorageSize = 256;        // iox2_port_factory_client_builder_storage_t
    private const int ServerBuilderStorageSize = 256;        // iox2_port_factory_server_builder_storage_t
    private const int ClientStorageSize = 248;               // iox2_client_storage_t
    private const int ServerStorageSize = 248;               // iox2_server_storage_t
    private const int RequestMutStorageSize = 80;            // iox2_request_mut_storage_t
    private const int ActiveRequestStorageSize = 128;        // iox2_active_request_storage_t
    private const int ResponseMutStorageSize = 88;           // iox2_response_mut_storage_t
    private const int ResponseStorageSize = 96;              // iox2_response_storage_t
    private const int PendingResponseStorageSize = 88;       // iox2_pending_response_storage_t

    [Fact] public void NodeBuilderStorage_MatchesCHeaderSize() => Assert.Equal(NodeBuilderStorageSize, Marshal.SizeOf<Iox2NativeMethods.iox2_node_builder_storage_t>());
    [Fact] public void NodeStorage_MatchesCHeaderSize() => Assert.Equal(NodeStorageSize, Marshal.SizeOf<Iox2NativeMethods.iox2_node_storage_t>());
    [Fact] public void NodeNameStorage_MatchesCHeaderSize() => Assert.Equal(NodeNameStorageSize, Marshal.SizeOf<Iox2NativeMethods.iox2_node_name_storage_t>());
    [Fact] public void ServiceNameStorage_MatchesCHeaderSize() => Assert.Equal(ServiceNameStorageSize, Marshal.SizeOf<Iox2NativeMethods.iox2_service_name_storage_t>());
    [Fact] public void ServiceBuilderStorage_MatchesCHeaderSize() => Assert.Equal(ServiceBuilderStorageSize, Marshal.SizeOf<Iox2NativeMethods.iox2_service_builder_storage_t>());
    [Fact] public void PortFactoryPubSubStorage_MatchesCHeaderSize() => Assert.Equal(PortFactoryPubSubStorageSize, Marshal.SizeOf<Iox2NativeMethods.iox2_port_factory_pub_sub_storage_t>());
    [Fact] public void PublisherBuilderStorage_MatchesCHeaderSize() => Assert.Equal(PublisherBuilderStorageSize, Marshal.SizeOf<Iox2NativeMethods.iox2_port_factory_publisher_builder_storage_t>());
    [Fact] public void SubscriberBuilderStorage_MatchesCHeaderSize() => Assert.Equal(SubscriberBuilderStorageSize, Marshal.SizeOf<Iox2NativeMethods.iox2_port_factory_subscriber_builder_storage_t>());
    [Fact] public void PublisherStorage_MatchesCHeaderSize() => Assert.Equal(PublisherStorageSize, Marshal.SizeOf<Iox2NativeMethods.iox2_publisher_storage_t>());
    [Fact] public void SubscriberStorage_MatchesCHeaderSize() => Assert.Equal(SubscriberStorageSize, Marshal.SizeOf<Iox2NativeMethods.iox2_subscriber_storage_t>());
    [Fact] public void SampleMutStorage_MatchesCHeaderSize() => Assert.Equal(SampleMutStorageSize, Marshal.SizeOf<Iox2NativeMethods.iox2_sample_mut_storage_t>());
    [Fact] public void SampleStorage_MatchesCHeaderSize() => Assert.Equal(SampleStorageSize, Marshal.SizeOf<Iox2NativeMethods.iox2_sample_storage_t>());
    [Fact] public void PortFactoryEventStorage_MatchesCHeaderSize() => Assert.Equal(PortFactoryEventStorageSize, Marshal.SizeOf<Iox2NativeMethods.iox2_port_factory_event_storage_t>());
    [Fact] public void NotifierBuilderStorage_MatchesCHeaderSize() => Assert.Equal(NotifierBuilderStorageSize, Marshal.SizeOf<Iox2NativeMethods.iox2_port_factory_notifier_builder_storage_t>());
    [Fact] public void ListenerBuilderStorage_MatchesCHeaderSize() => Assert.Equal(ListenerBuilderStorageSize, Marshal.SizeOf<Iox2NativeMethods.iox2_port_factory_listener_builder_storage_t>());
    [Fact] public void NotifierStorage_MatchesCHeaderSize() => Assert.Equal(NotifierStorageSize, Marshal.SizeOf<Iox2NativeMethods.iox2_notifier_storage_t>());
    [Fact] public void ListenerStorage_MatchesCHeaderSize() => Assert.Equal(ListenerStorageSize, Marshal.SizeOf<Iox2NativeMethods.iox2_listener_storage_t>());
    [Fact] public void PortFactoryReqRespStorage_MatchesCHeaderSize() => Assert.Equal(PortFactoryReqRespStorageSize, Marshal.SizeOf<Iox2NativeMethods.iox2_port_factory_request_response_storage_t>());
    [Fact] public void ClientBuilderStorage_MatchesCHeaderSize() => Assert.Equal(ClientBuilderStorageSize, Marshal.SizeOf<Iox2NativeMethods.iox2_port_factory_client_builder_storage_t>());
    [Fact] public void ServerBuilderStorage_MatchesCHeaderSize() => Assert.Equal(ServerBuilderStorageSize, Marshal.SizeOf<Iox2NativeMethods.iox2_port_factory_server_builder_storage_t>());
    [Fact] public void ClientStorage_MatchesCHeaderSize() => Assert.Equal(ClientStorageSize, Marshal.SizeOf<Iox2NativeMethods.iox2_client_storage_t>());
    [Fact] public void ServerStorage_MatchesCHeaderSize() => Assert.Equal(ServerStorageSize, Marshal.SizeOf<Iox2NativeMethods.iox2_server_storage_t>());
    [Fact] public void RequestMutStorage_MatchesCHeaderSize() => Assert.Equal(RequestMutStorageSize, Marshal.SizeOf<Iox2NativeMethods.iox2_request_mut_storage_t>());
    [Fact] public void ActiveRequestStorage_MatchesCHeaderSize() => Assert.Equal(ActiveRequestStorageSize, Marshal.SizeOf<Iox2NativeMethods.iox2_active_request_storage_t>());
    [Fact] public void ResponseMutStorage_MatchesCHeaderSize() => Assert.Equal(ResponseMutStorageSize, Marshal.SizeOf<Iox2NativeMethods.iox2_response_mut_storage_t>());
    [Fact] public void ResponseStorage_MatchesCHeaderSize() => Assert.Equal(ResponseStorageSize, Marshal.SizeOf<Iox2NativeMethods.iox2_response_storage_t>());
    [Fact] public void PendingResponseStorage_MatchesCHeaderSize() => Assert.Equal(PendingResponseStorageSize, Marshal.SizeOf<Iox2NativeMethods.iox2_pending_response_storage_t>());
}