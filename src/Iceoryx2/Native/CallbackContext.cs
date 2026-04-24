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
using System.Runtime.InteropServices;

namespace Iceoryx2.Native;

/// <summary>
/// Pins managed state so it can be passed through an FFI boundary as an opaque
/// <see cref="IntPtr"/> context and recovered inside a native callback.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// var ctx = CallbackContext.Pin(state);
/// try { NativeCall(..., Trampoline, ctx); }
/// finally { CallbackContext.Unpin&lt;TState&gt;(ctx); }
///
/// // Inside the static trampoline:
/// var state = CallbackContext.Peek&lt;TState&gt;(contextPtr);
/// </code>
/// Every <see cref="Pin{T}"/> MUST be matched by exactly one <see cref="Unpin{T}"/>.
/// </remarks>
internal static class CallbackContext
{
    /// <summary>
    /// Allocates a normal <see cref="GCHandle"/> for <paramref name="state"/> and
    /// returns its <see cref="IntPtr"/> token, suitable for passing through FFI.
    /// </summary>
    public static IntPtr Pin<T>(T state) where T : class
    {
        ArgumentNullException.ThrowIfNull(state);
        var handle = GCHandle.Alloc(state, GCHandleType.Normal);
        return GCHandle.ToIntPtr(handle);
    }

    /// <summary>
    /// Recovers the pinned state without freeing the handle. Safe from a callback.
    /// Returns <c>null</c> when <paramref name="token"/> is <see cref="IntPtr.Zero"/>
    /// or the target is not of type <typeparamref name="T"/>.
    /// </summary>
    public static T? Peek<T>(IntPtr token) where T : class
    {
        if (token == IntPtr.Zero)
            return null;
        var handle = GCHandle.FromIntPtr(token);
        return handle.IsAllocated ? handle.Target as T : null;
    }

    /// <summary>
    /// Frees the <see cref="GCHandle"/> and returns the (now unrooted) state.
    /// MUST be called exactly once per <see cref="Pin{T}"/>, in a <c>finally</c>.
    /// </summary>
    /// <remarks>
    /// The returned value is provided to enable round-trip testing of the
    /// pin/unpin contract. Production callers typically discard it because the
    /// state object is already rooted on the calling stack frame.
    /// </remarks>
    public static T? Unpin<T>(IntPtr token) where T : class
    {
        if (token == IntPtr.Zero)
            return null;
        var handle = GCHandle.FromIntPtr(token);
        if (!handle.IsAllocated)
            return null;
        var target = handle.Target as T;
        handle.Free();
        return target;
    }
}