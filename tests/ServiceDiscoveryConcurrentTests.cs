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
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Iceoryx2.Tests;

public class ServiceDiscoveryConcurrentTests
{
    [Fact]
    public async Task Node_List_ConcurrentCalls_EachReturnsOwnResults()
    {
        const int parallelism = 16;
        const int callsPerNode = 4;

        var tasks = Enumerable.Range(0, parallelism).Select(i => Task.Run(() =>
        {
            using var node = NodeBuilder.New()
                .Name($"concurrent_list_node_{i}")
                .Create()
                .Expect($"Failed to create node {i}");

            var results = new List<List<ServiceStaticConfig>>();
            for (int c = 0; c < callsPerNode; c++)
            {
                var r = node.List();
                Assert.True(r.IsOk, $"List() failed on node {i} call {c}");
                results.Add(r.Expect("Expected Ok"));
            }
            return results;
        })).ToArray();

        var allResults = await Task.WhenAll(tasks);

        // Each task's four results are independent List<> instances — identity
        // check rather than content: the old static-field implementation made
        // every concurrent caller share the SAME list.
        foreach (var perTaskResults in allResults)
        {
            var ids = perTaskResults.Select(r => (object)r).ToHashSet();
            Assert.Equal(perTaskResults.Count, ids.Count);
        }

        // No list is shared across tasks.
        var allLists = allResults.SelectMany(r => r).Cast<object>().ToList();
        Assert.Equal(allLists.Count, allLists.ToHashSet().Count);
    }

    [Fact]
    public async Task Node_List_ConcurrentCalls_WithCreatedServices_EachObservesItsOwnService()
    {
        // Unique names prevent leftover shared memory from confusing the test.
        var serviceA = $"test_concurrent_list_a_{Guid.NewGuid():N}";
        var serviceB = $"test_concurrent_list_b_{Guid.NewGuid():N}";

        using var nodeA = NodeBuilder.New().Name("list_concurrent_a").Create().Expect("Node A");
        using var nodeB = NodeBuilder.New().Name("list_concurrent_b").Create().Expect("Node B");

        using var svcA = nodeA.ServiceBuilder().PublishSubscribe<int>().Open(serviceA).Expect("Service A");
        using var svcB = nodeB.ServiceBuilder().PublishSubscribe<int>().Open(serviceB).Expect("Service B");

        // Drive many concurrent List() calls; each should find its own service.
        var taskA = Task.Run(() =>
        {
            for (int i = 0; i < 20; i++)
            {
                var list = nodeA.List().Expect("A.List");
                Assert.Contains(list, c => c.Name.Contains(serviceA));
            }
        });
        var taskB = Task.Run(() =>
        {
            for (int i = 0; i < 20; i++)
            {
                var list = nodeB.List().Expect("B.List");
                Assert.Contains(list, c => c.Name.Contains(serviceB));
            }
        });

        await Task.WhenAll(taskA, taskB);
    }
}
