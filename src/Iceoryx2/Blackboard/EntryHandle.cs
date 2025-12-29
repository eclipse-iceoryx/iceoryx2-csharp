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

using Iceoryx2.SafeHandles;
using System;
using System.Runtime.InteropServices;
using static Iceoryx2.Native.Iox2NativeMethods;

namespace Iceoryx2.Blackboard;

/// <summary>
/// A read-only entry handle for accessing a value in the blackboard.
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <typeparam name="TValue">The type of the value.</typeparam>
public sealed class EntryHandle<TKey, TValue> : IDisposable
    where TKey : unmanaged
    where TValue : unmanaged
{
    private readonly SafeEntryHandleHandle _handle;
    private readonly TKey _key;
    private bool _disposed;

    internal EntryHandle(IntPtr handle, TKey key)
    {
        _handle = new SafeEntryHandleHandle(handle);
        _key = key;
    }

    /// <summary>
    /// Gets the key associated with this entry handle.
    /// </summary>
    public TKey Key => _key;

    /// <summary>
    /// Gets the current value from the blackboard entry.
    /// </summary>
    /// <returns>A Result containing the current value or an error.</returns>
    public unsafe Result<TValue, Iox2Error> Get()
    {
        ThrowIfDisposed();

        var valueSize = (ulong)sizeof(TValue);
        var valueAlignment = GetAlignment<TValue>(valueSize);

        // Allocate memory for the value
        var valuePtr = Marshal.AllocHGlobal(sizeof(TValue));
        try
        {
            var handlePtr = _handle.DangerousGetHandle();
            // Note: iox2_entry_handle_get returns void
            iox2_entry_handle_get(
                ref handlePtr,
                valuePtr,
                valueSize,
                valueAlignment);

            // Use unsafe pointer dereference instead of Marshal.PtrToStructure
            var value = *(TValue*)valuePtr;
            return Result<TValue, Iox2Error>.Ok(value);
        }
        finally
        {
            Marshal.FreeHGlobal(valuePtr);
        }
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

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(EntryHandle<TKey, TValue>));
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="EntryHandle{TKey, TValue}"/>.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _handle?.Dispose();
            _disposed = true;
        }
    }
}