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

using Iceoryx2;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Iceoryx2.Tests.Ffi;

public class WaitSetConcurrentTests
{
    [Fact]
    public async Task WaitSet_ConcurrentInstances_DoNotInterfere()
    {
        // Each task owns a WaitSet + notifier + listener on its own event service.
        // The trampoline must deliver each notification only to the WaitSet that
        // was waiting for it — no cross-contamination across instances.
        const int instanceCount = 4;

        var tasks = new Task<int>[instanceCount];
        for (int i = 0; i < instanceCount; i++)
        {
            int local = i;
            tasks[local] = Task.Run(() =>
            {
                var serviceName = $"test_waitset_concurrent_{local}_{Guid.NewGuid():N}";
                using var node = NodeBuilder.New().Name($"wsc_node_{local}").Create().Expect("node");
                using var evtService = node.ServiceBuilder().Event().Open(serviceName).Expect("service");
                using var notifier = evtService.CreateNotifier().Expect("notifier");
                using var listener = evtService.CreateListener().Expect("listener");
                using var waitset = WaitSetBuilder.New().Create().Expect("waitset");
                using var guard = waitset.AttachNotification(listener).Expect("guard");

                notifier.Notify(new EventId((ulong)local)).Expect("notify");

                int hits = 0;
                var runResult = waitset.WaitAndProcessOnce(attachmentId =>
                {
                    hits++;
                    return CallbackProgression.Stop;
                });
                Assert.True(runResult.IsOk);
                return hits;
            });
        }

        var hits = await Task.WhenAll(tasks);
        foreach (var h in hits)
            Assert.Equal(1, h);
    }

    [Fact]
    public void WaitSet_TrampolineReturnsStop_WhenCallbackThrows()
    {
        var serviceName = $"test_waitset_throw_{Guid.NewGuid():N}";
        using var node = NodeBuilder.New().Name("wsc_throw_node").Create().Expect("node");
        using var evtService = node.ServiceBuilder().Event().Open(serviceName).Expect("service");
        using var notifier = evtService.CreateNotifier().Expect("notifier");
        using var listener = evtService.CreateListener().Expect("listener");
        using var waitset = WaitSetBuilder.New().Create().Expect("waitset");
        using var guard = waitset.AttachNotification(listener).Expect("guard");

        notifier.Notify(new EventId(0UL)).Expect("notify");

        // A throwing callback must not propagate across the FFI boundary.
        var result = waitset.WaitAndProcessOnce(_ => throw new InvalidOperationException("boom"));

        // WaitAndProcessOnce should still return Ok — the trampoline swallowed
        // the exception and returned STOP, and the native call succeeded.
        Assert.True(result.IsOk, "WaitAndProcessOnce must not propagate user exceptions across FFI.");
    }
}