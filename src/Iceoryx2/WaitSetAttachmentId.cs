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

namespace Iceoryx2;

/// <summary>
/// Identifies which attachment triggered an event in the WaitSet.
/// Used in WaitSet callbacks to determine the source of an event.
/// </summary>
public sealed class WaitSetAttachmentId : IDisposable, IEquatable<WaitSetAttachmentId>, IComparable<WaitSetAttachmentId>
{
    private SafeWaitSetAttachmentIdHandle _handle;
    private bool _disposed;

    internal WaitSetAttachmentId(SafeWaitSetAttachmentIdHandle handle)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
    }

    /// <summary>
    /// Checks if this attachment ID corresponds to the given guard.
    /// Use this to determine which attachment triggered the event.
    /// </summary>
    /// <param name="guard">The guard to check against.</param>
    /// <returns>True if the event originated from the given guard.</returns>
    public bool HasEventFrom(WaitSetGuard guard)
    {
        ThrowIfDisposed();
        if (guard == null)
            throw new ArgumentNullException(nameof(guard));

        var attachmentHandle = _handle.DangerousGetHandle();
        var guardHandle = guard.GetHandle();

        return Native.Iox2NativeMethods.iox2_waitset_attachment_id_has_event_from(
            ref attachmentHandle,
            ref guardHandle);
    }

    /// <summary>
    /// Checks if the deadline associated with the given guard was missed.
    /// Only relevant for guards created with AttachDeadline().
    /// </summary>
    /// <param name="guard">The deadline guard to check.</param>
    /// <returns>True if the deadline was missed.</returns>
    public bool HasMissedDeadline(WaitSetGuard guard)
    {
        ThrowIfDisposed();
        if (guard == null)
            throw new ArgumentNullException(nameof(guard));

        var attachmentHandle = _handle.DangerousGetHandle();
        var guardHandle = guard.GetHandle();

        return Native.Iox2NativeMethods.iox2_waitset_attachment_id_has_missed_deadline(
            ref attachmentHandle,
            ref guardHandle);
    }

    /// <summary>
    /// Checks equality between two attachment IDs.
    /// </summary>
    public bool Equals(WaitSetAttachmentId? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;

        ThrowIfDisposed();
        other.ThrowIfDisposed();

        var thisHandle = _handle.DangerousGetHandle();
        var otherHandle = other._handle.DangerousGetHandle();

        return Native.Iox2NativeMethods.iox2_waitset_attachment_id_equal(
            ref thisHandle,
            ref otherHandle);
    }

    /// <summary>
    /// Compares this attachment ID with another for ordering.
    /// </summary>
    public int CompareTo(WaitSetAttachmentId? other)
    {
        if (other is null)
            return 1;
        if (ReferenceEquals(this, other))
            return 0;

        ThrowIfDisposed();
        other.ThrowIfDisposed();

        var thisHandle = _handle.DangerousGetHandle();
        var otherHandle = other._handle.DangerousGetHandle();

        if (Native.Iox2NativeMethods.iox2_waitset_attachment_id_less(ref thisHandle, ref otherHandle))
            return -1;
        if (Native.Iox2NativeMethods.iox2_waitset_attachment_id_less(ref otherHandle, ref thisHandle))
            return 1;
        return 0;
    }

    /// <summary>
    /// Checks equality with an object.
    /// </summary>
    public override bool Equals(object? obj) => Equals(obj as WaitSetAttachmentId);

    /// <summary>
    /// Gets the hash code.
    /// </summary>
    public override int GetHashCode() => _handle.DangerousGetHandle().GetHashCode();

    /// <summary>
    /// Equality comparison operator.
    /// </summary>
    public static bool operator ==(WaitSetAttachmentId? left, WaitSetAttachmentId? right)
    {
        if (left is null)
            return right is null;
        return left.Equals(right);
    }

    /// <summary>
    /// Inequality comparison operator.
    /// </summary>
    public static bool operator !=(WaitSetAttachmentId? left, WaitSetAttachmentId? right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Disposes the attachment ID.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _handle?.Dispose();
            _disposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WaitSetAttachmentId));
    }
}