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

namespace Iceoryx2;

/// <summary>
/// Represents an event that occurred on a source attached to a WaitSet.
/// Contains the attachment ID which can be compared against guards to determine the source.
/// This struct implements IDisposable because it owns the WaitSetAttachmentId.
/// </summary>
public readonly struct WaitSetEvent : IDisposable
{
    /// <summary>
    /// Gets the attachment ID that identifies which attached source triggered this event.
    /// Compare this against your WaitSetGuard instances using HasEventFrom() to determine the source.
    /// </summary>
    public WaitSetAttachmentId AttachmentId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WaitSetEvent"/> struct.
    /// </summary>
    /// <param name="attachmentId">The attachment ID that triggered this event.</param>
    public WaitSetEvent(WaitSetAttachmentId attachmentId)
    {
        AttachmentId = attachmentId ?? throw new ArgumentNullException(nameof(attachmentId));
    }

    /// <summary>
    /// Checks if this event originated from the given guard.
    /// </summary>
    /// <param name="guard">The guard to check against.</param>
    /// <returns>True if the event came from this guard.</returns>
    public bool IsFrom(WaitSetGuard guard)
    {
        return AttachmentId.HasEventFrom(guard);
    }

    /// <summary>
    /// Checks if this event represents a missed deadline for the given guard.
    /// Only relevant for deadline attachments.
    /// </summary>
    /// <param name="guard">The deadline guard to check.</param>
    /// <returns>True if the deadline was missed.</returns>
    public bool HasMissedDeadline(WaitSetGuard guard)
    {
        return AttachmentId.HasMissedDeadline(guard);
    }

    /// <summary>
    /// Disposes the attachment ID.
    /// </summary>
    public void Dispose()
    {
        AttachmentId?.Dispose();
    }

    /// <summary>
    /// Returns a string representation of this event.
    /// </summary>
    public override string ToString()
    {
        return $"WaitSetEvent {{ AttachmentId = {AttachmentId} }}";
    }
}