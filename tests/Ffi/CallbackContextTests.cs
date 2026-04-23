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
}
