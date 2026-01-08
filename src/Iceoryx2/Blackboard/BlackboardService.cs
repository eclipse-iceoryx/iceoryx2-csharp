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
/// Represents a blackboard service port factory.
/// The blackboard pattern provides a shared-memory key-value repository that can be
/// modified by one writer and read by many readers.
/// </summary>
/// <typeparam name="TKey">The type of keys in the blackboard.</typeparam>
public sealed class BlackboardService<TKey> : IDisposable
    where TKey : unmanaged
{
    private readonly SafeBlackboardServiceHandle _handle;
    private readonly Func<TKey, TKey, bool> _keyComparer;
    private bool _disposed;

    internal BlackboardService(IntPtr handle, Func<TKey, TKey, bool> keyComparer)
    {
        _handle = new SafeBlackboardServiceHandle(handle);
        _keyComparer = keyComparer;
    }

    /// <summary>
    /// Creates a new writer for this blackboard service.
    /// Note: Only one writer is allowed per blackboard service.
    /// </summary>
    /// <returns>A Result containing the writer or an error.</returns>
    public Result<Writer<TKey>, Iox2Error> CreateWriter()
    {
        ThrowIfDisposed();

        var handlePtr = _handle.DangerousGetHandle();
        var writerBuilderHandle = iox2_port_factory_blackboard_writer_builder(
            ref handlePtr,
            IntPtr.Zero);

        if (writerBuilderHandle == IntPtr.Zero)
        {
            return Result<Writer<TKey>, Iox2Error>.Err(Iox2Error.WriterCreationFailed);
        }

        var result = iox2_port_factory_writer_builder_create(
            writerBuilderHandle,
            IntPtr.Zero,
            out var writerHandle);

        if (result != IOX2_OK)
        {
            return Result<Writer<TKey>, Iox2Error>.Err(Iox2Error.WriterCreationFailed);
        }

        return Result<Writer<TKey>, Iox2Error>.Ok(new Writer<TKey>(writerHandle, _keyComparer));
    }

    /// <summary>
    /// Creates a new reader for this blackboard service.
    /// Multiple readers are allowed per blackboard service.
    /// </summary>
    /// <returns>A Result containing the reader or an error.</returns>
    public Result<Reader<TKey>, Iox2Error> CreateReader()
    {
        ThrowIfDisposed();

        var handlePtr = _handle.DangerousGetHandle();
        var readerBuilderHandle = iox2_port_factory_blackboard_reader_builder(
            ref handlePtr,
            IntPtr.Zero);

        if (readerBuilderHandle == IntPtr.Zero)
        {
            return Result<Reader<TKey>, Iox2Error>.Err(Iox2Error.ReaderCreationFailed);
        }

        var result = iox2_port_factory_reader_builder_create(
            readerBuilderHandle,
            IntPtr.Zero,
            out var readerHandle);

        if (result != IOX2_OK)
        {
            return Result<Reader<TKey>, Iox2Error>.Err(Iox2Error.ReaderCreationFailed);
        }

        return Result<Reader<TKey>, Iox2Error>.Ok(new Reader<TKey>(readerHandle, _keyComparer));
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(BlackboardService<TKey>));
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="BlackboardService{TKey}"/>.
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