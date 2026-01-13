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
/// A reader for accessing values in a blackboard service.
/// Multiple readers can exist per blackboard service.
/// </summary>
/// <typeparam name="TKey">The type of keys in the blackboard.</typeparam>
/// <remarks>
/// Thread Safety: This class is not thread-safe. Each reader instance should be used from a single thread only.
/// Multiple reader instances can safely access the same blackboard concurrently from different threads.
/// </remarks>
public sealed class Reader<TKey> : IDisposable
    where TKey : unmanaged
{
    private readonly SafeReaderHandle _handle;
    private readonly Func<TKey, TKey, bool> _keyComparer;
    private bool _disposed;

    internal Reader(IntPtr handle, Func<TKey, TKey, bool> keyComparer)
    {
        _handle = new SafeReaderHandle(handle);
        _keyComparer = keyComparer;
    }

    /// <summary>
    /// Gets a read-only entry handle for the specified key and value type.
    /// The entry handle can be used to read values from the blackboard.
    /// </summary>
    /// <typeparam name="TValue">The value type for this key.</typeparam>
    /// <param name="key">The key to get the entry handle for.</param>
    /// <returns>A Result containing the entry handle or an error.</returns>
    public unsafe Result<EntryHandle<TKey, TValue>, Iox2Error> Entry<TValue>(TKey key)
        where TValue : unmanaged
    {
        ThrowIfDisposed();

        var valueTypeName = ServiceBuilder.GetRustCompatibleTypeName<TValue>();
        var valueTypeSize = (ulong)sizeof(TValue);
        var valueTypeAlignment = BlackboardHelpers.GetAlignment<TValue>(valueTypeSize);

        // Stack allocate - matches how Writer passes key directly
        TKey* keyPtr = stackalloc TKey[1];
        keyPtr[0] = key;

        var handlePtr = _handle.DangerousGetHandle();
        var result = iox2_reader_entry(
            ref handlePtr,
            IntPtr.Zero,
            out var entryHandlePtr,
            (IntPtr)keyPtr,
            valueTypeName,
            valueTypeName.Length,
            valueTypeSize,
            valueTypeAlignment);

        if (result != IOX2_OK || entryHandlePtr == IntPtr.Zero)
        {
            return Result<EntryHandle<TKey, TValue>, Iox2Error>.Err(Iox2Error.EntryAccessFailed);
        }

        return Result<EntryHandle<TKey, TValue>, Iox2Error>.Ok(
            new EntryHandle<TKey, TValue>(entryHandlePtr, key));
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(Reader<TKey>));
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="Reader{TKey}"/>.
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