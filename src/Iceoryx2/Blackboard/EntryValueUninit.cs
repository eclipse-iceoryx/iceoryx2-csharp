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
using static Iceoryx2.Native.Iox2NativeMethods;

namespace Iceoryx2.Blackboard;

/// <summary>
/// Represents a loaned uninitialized entry value that can be written to and then committed to the blackboard.
/// This is useful for in-place construction of values in shared memory.
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <typeparam name="TValue">The type of the value.</typeparam>
public sealed class EntryValueUninit<TKey, TValue> : IDisposable
    where TKey : unmanaged
    where TValue : unmanaged
{
    private readonly SafeEntryValueUninitHandle _handle;
    private readonly TKey _key;
    private bool _disposed;

    internal EntryValueUninit(IntPtr handle, TKey key)
    {
        _handle = new SafeEntryValueUninitHandle(handle);
        _key = key;
    }

    /// <summary>
    /// Gets the key associated with this entry value.
    /// </summary>
    public TKey Key => _key;

    /// <summary>
    /// Gets a mutable reference to the value for in-place modification.
    /// </summary>
    /// <returns>A reference to the value.</returns>
    public unsafe ref TValue ValueMut()
    {
        ThrowIfDisposed();

        var handlePtr = _handle.DangerousGetHandle();
        iox2_entry_value_uninit_value_mut(ref handlePtr, out var payloadPtr);
        if (payloadPtr == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to get payload pointer from entry value.");
        }
        return ref *(TValue*)payloadPtr;
    }

    /// <summary>
    /// Updates the entry value by copying the provided value to the loaned memory.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public unsafe void UpdateWithCopy(TValue value)
    {
        ThrowIfDisposed();

        var handlePtr = _handle.DangerousGetHandle();
        iox2_entry_value_uninit_value_mut(ref handlePtr, out var payloadPtr);
        if (payloadPtr == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to get payload pointer from entry value.");
        }
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

        // Consume the handle - this marks it as invalid so ReleaseHandle won't be called
        var rawHandle = _handle.Consume();

        // Note: iox2_entry_value_uninit_update returns void
        iox2_entry_value_uninit_update(
            rawHandle,
            IntPtr.Zero,
            out var entryHandleMutPtr);

        if (entryHandleMutPtr == IntPtr.Zero)
        {
            return Result<EntryHandleMut<TKey, TValue>, Iox2Error>.Err(Iox2Error.EntryAccessFailed);
        }

        return Result<EntryHandleMut<TKey, TValue>, Iox2Error>.Ok(
            new EntryHandleMut<TKey, TValue>(entryHandleMutPtr, _key));
    }

    private void ThrowIfDisposed()
    {
        if (_disposed || _handle.IsInvalid)
        {
            throw new ObjectDisposedException(nameof(EntryValueUninit<TKey, TValue>));
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="EntryValueUninit{TKey, TValue}"/>.
    /// If the entry value has not been committed, it will be dropped without updating the blackboard.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _handle.Dispose();
            _disposed = true;
        }
    }
}