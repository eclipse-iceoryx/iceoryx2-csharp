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

using System.Runtime.InteropServices;
using Iceoryx2.Native;
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
    private const int ServiceBuilderStorageSize = 9104;   // iceoryx2.h iox2_service_builder_storage_t
    private const int NodeBuilderStorageSize = 18696;     // iceoryx2.h iox2_node_builder_storage_t

    [Fact]
    public void NodeBuilderStorage_MatchesCHeaderSize()
    {
        Assert.Equal(NodeBuilderStorageSize, Marshal.SizeOf<Iox2NativeMethods.iox2_node_builder_storage_t>());
    }

    [Fact]
    public void ServiceBuilderStorage_MatchesCHeaderSize()
    {
        Assert.Equal(ServiceBuilderStorageSize, Marshal.SizeOf<Iox2NativeMethods.iox2_service_builder_storage_t>());
    }
}
