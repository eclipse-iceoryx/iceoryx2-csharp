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

using Xunit;

namespace Iceoryx2.Tests;

public class NodeTests
{
    [Fact]
    public void CanCreateNodeBuilder()
    {
        var builder = NodeBuilder.New();
        Assert.NotNull(builder);
    }

    [Fact]
    public void CanSetNodeName()
    {
        var builder = NodeBuilder.New().Name("test_node");
        Assert.NotNull(builder);
    }

    [Fact]
    public void CanCreateNode()
    {
        var result = NodeBuilder.New().Create();

        Assert.True(result.IsOk);
        using var node = result.Unwrap();
        Assert.NotNull(node);
    }

    [Fact]
    public void NodeHasName()
    {
        var result = NodeBuilder.New().Create();

        Assert.True(result.IsOk);
        using var node = result.Unwrap();
        // Note: Name property currently returns placeholder "node"
        // TODO: Implement proper node name retrieval from C FFI
        Assert.NotNull(node.Name);
    }
}