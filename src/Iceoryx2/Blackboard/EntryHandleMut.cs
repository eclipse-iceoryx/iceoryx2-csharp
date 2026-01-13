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
/// A mutable entry handle for modifying a value in the blackboard.
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <typeparam name="TValue">The type of the value.</typeparam>
public sealed class EntryHandleMut<TKey, TValue> : IDisposable
    where TKey : unmanaged
    where TValue : unmanaged
{
    private readonly SafeEntryHandleMutHandle _handle;
    private readonly TKey _key;
    private bool _disposed;

    internal EntryHandleMut(IntPtr handle, TKey key)
    {
        _handle = new SafeEntryHandleMutHandle(handle);
        _key = key;
    }

    /// <summary>
    /// Gets the key associated with this entry handle.
    /// </summary>
    public TKey Key => _key;

    /// <summary>
    /// Updates the entry value by copying the provided value.
    /// </summary>
    /// <param name="value">The new value to set.</param>
    /// <returns>A Result indicating success or an error.</returns>
    public unsafe Result<Unit, Iox2Error> Update(TValue value)
    {
        ThrowIfDisposed();

        var valueSize = (ulong)sizeof(TValue);
        var valueAlignment = BlackboardHelpers.GetAlignment<TValue>(valueSize);

        // Stack allocate for small fixed-size value
        TValue* valuePtr = stackalloc TValue[1];
        *valuePtr = value;

        var handlePtr = _handle.DangerousGetHandle();
        // Note: iox2_entry_handle_mut_update_with_copy returns void
        iox2_entry_handle_mut_update_with_copy(
            ref handlePtr,
            (IntPtr)valuePtr,
            valueSize,
            valueAlignment);

        return Result<Unit, Iox2Error>.Ok(Unit.Value);
    }

    /// <summary>
    /// Loans an uninitialized entry value for in-place construction.
    /// This is useful when you want to construct the value directly in shared memory.
    /// </summary>
    /// <returns>A Result containing the loaned entry value or an error.</returns>
    public unsafe Result<EntryValue<TKey, TValue>, Iox2Error> LoanUninit()
    {
        ThrowIfDisposed();

        var valueSize = (ulong)sizeof(TValue);
        var valueAlignment = BlackboardHelpers.GetAlignment<TValue>(valueSize);

        var handlePtr = _handle.DangerousGetHandle();
        // Note: iox2_entry_handle_mut_loan_uninit returns void
        iox2_entry_handle_mut_loan_uninit(
            handlePtr,
            IntPtr.Zero,
            out var entryValueHandle,
            valueSize,
            valueAlignment);

        if (entryValueHandle == IntPtr.Zero)
        {
            return Result<EntryValue<TKey, TValue>, Iox2Error>.Err(Iox2Error.EntryAccessFailed);
        }

        return Result<EntryValue<TKey, TValue>, Iox2Error>.Ok(
            new EntryValue<TKey, TValue>(entryValueHandle, _key));
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(EntryHandleMut<TKey, TValue>));
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="EntryHandleMut{TKey, TValue}"/>.
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