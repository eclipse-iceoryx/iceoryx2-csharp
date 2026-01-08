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
using static Iceoryx2.Native.Iox2NativeMethods;

namespace Iceoryx2.Blackboard;

/// <summary>
/// Represents a loaned entry value that can be written to and then committed to the blackboard.
/// This is useful for in-place construction of values in shared memory.
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <typeparam name="TValue">The type of the value.</typeparam>
public sealed class EntryValue<TKey, TValue> : IDisposable
    where TKey : unmanaged
    where TValue : unmanaged
{
    private IntPtr _handle;
    private readonly TKey _key;
    private bool _disposed;
    private bool _committed;

    internal EntryValue(IntPtr handle, TKey key)
    {
        _handle = handle;
        _key = key;
    }

    /// <summary>
    /// Gets the key associated with this entry value.
    /// </summary>
    public TKey Key => _key;

    /// <summary>
    /// Gets a mutable reference to the payload for in-place modification.
    /// </summary>
    /// <returns>A reference to the payload.</returns>
    public unsafe ref TValue PayloadMut()
    {
        ThrowIfDisposed();
        ThrowIfCommitted();

        iox2_entry_value_mut(ref _handle, out var payloadPtr);
        return ref *(TValue*)payloadPtr;
    }

    /// <summary>
    /// Writes a value to the loaned memory.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public unsafe void Write(TValue value)
    {
        ThrowIfDisposed();
        ThrowIfCommitted();

        iox2_entry_value_mut(ref _handle, out var payloadPtr);
        // Use unsafe pointer dereference instead of Marshal.StructureToPtr
        *(TValue*)payloadPtr = value;
    }

    /// <summary>
    /// Commits the entry value to the blackboard.
    /// After calling this method, the entry value is no longer usable.
    /// </summary>
    /// <returns>A Result containing a new mutable entry handle or an error.</returns>
    public Result<EntryHandleMut<TKey, TValue>, Iox2Error> Update()
    {
        ThrowIfDisposed();
        ThrowIfCommitted();

        // Note: iox2_entry_value_update returns void
        iox2_entry_value_update(
            _handle,
            IntPtr.Zero,
            out var entryHandleMutPtr);

        _committed = true;
        _handle = IntPtr.Zero; // Handle is consumed by update

        if (entryHandleMutPtr == IntPtr.Zero)
        {
            return Result<EntryHandleMut<TKey, TValue>, Iox2Error>.Err(Iox2Error.EntryAccessFailed);
        }

        return Result<EntryHandleMut<TKey, TValue>, Iox2Error>.Ok(
            new EntryHandleMut<TKey, TValue>(entryHandleMutPtr, _key));
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(EntryValue<TKey, TValue>));
        }
    }

    private void ThrowIfCommitted()
    {
        if (_committed)
        {
            throw new InvalidOperationException("Entry value has already been committed.");
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="EntryValue{TKey, TValue}"/>.
    /// If the entry value has not been committed, it will be dropped without updating the blackboard.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            if (!_committed && _handle != IntPtr.Zero)
            {
                iox2_entry_value_drop(_handle);
            }
            _disposed = true;
        }
    }
}