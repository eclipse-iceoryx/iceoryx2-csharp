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
using Iceoryx2.Native;
using Xunit;

namespace Iceoryx2.Tests.Ffi;

public class CallbackContextTests
{
    [Fact]
    public void Pin_Then_Unpin_Returns_Same_Instance()
    {
        var state = new List<int> { 1, 2, 3 };

        IntPtr token = CallbackContext.Pin(state);
        Assert.NotEqual(IntPtr.Zero, token);

        var recovered = CallbackContext.Unpin<List<int>>(token);
        Assert.Same(state, recovered);
    }

    [Fact]
    public void Peek_Returns_Same_Instance_Without_Freeing()
    {
        var state = new List<int> { 42 };
        IntPtr token = CallbackContext.Pin(state);
        try
        {
            var first = CallbackContext.Peek<List<int>>(token);
            var second = CallbackContext.Peek<List<int>>(token);
            Assert.Same(state, first);
            Assert.Same(state, second);
        }
        finally
        {
            CallbackContext.Unpin<List<int>>(token);
        }
    }

    [Fact]
    public void Peek_With_Zero_Token_Returns_Null()
    {
        var result = CallbackContext.Peek<List<int>>(IntPtr.Zero);
        Assert.Null(result);
    }

    [Fact]
    public void Unpin_With_Zero_Token_Returns_Null()
    {
        var result = CallbackContext.Unpin<List<int>>(IntPtr.Zero);
        Assert.Null(result);
    }

    [Fact]
    public void Pin_Unpin_Repeated_Does_Not_Leak_GCHandles()
    {
        // Ten thousand iterations. If Unpin doesn't Free the GCHandle, we would
        // hold 10k strong references to the allocated lists and GC would not
        // reclaim them. We use a weak reference to the first allocation as a
        // tripwire: after the loop + GC, it should be collectible.
        WeakReference weak;
        {
            var first = new List<int>();
            weak = new WeakReference(first);
            var token = CallbackContext.Pin(first);
            CallbackContext.Unpin<List<int>>(token);
        }

        for (int i = 0; i < 10_000; i++)
        {
            var t = CallbackContext.Pin(new List<int>());
            CallbackContext.Unpin<List<int>>(t);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(weak.IsAlive, "Unpin must free the GCHandle so the state can be collected.");
    }
}
