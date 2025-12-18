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

namespace Iceoryx2.ErrorHandling
{
    /// <summary>
    /// Represents an error that occurred during a notify operation.
    /// Notify operations trigger events that listeners wait for.
    /// </summary>
    /// <remarks>
    /// Common causes:
    /// <list type="bullet">
    /// <item>Event ID exceeds maximum allowed value</item>
    /// <item>Notifier is in invalid state</item>
    /// <item>No active listeners</item>
    /// <item>System resource error</item>
    /// </list>
    /// </remarks>
    public class NotifyError : Iox2Error
    {
        /// <summary>
        /// Gets the event ID that failed to notify, if available.
        /// </summary>
        public EventId? EventId { get; }

        /// <summary>
        /// Gets the error kind for pattern matching.
        /// </summary>
        public override Iox2ErrorKind Kind => Iox2ErrorKind.NotifyFailed;

        /// <summary>
        /// Gets additional details about why the notify operation failed.
        /// </summary>
        public override string? Details { get; }

        /// <summary>
        /// Gets a human-readable error message including event ID if available.
        /// </summary>
        public override string Message
        {
            get
            {
                var msg = EventId.HasValue
                    ? $"Failed to notify event {EventId.Value}"
                    : "Failed to notify event";
                return Details != null ? $"{msg}. Details: {Details}" : $"{msg}.";
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotifyError"/> class.
        /// </summary>
        /// <param name="eventId">The event ID that failed to notify.</param>
        /// <param name="details">Optional details about the error.</param>
        public NotifyError(EventId? eventId = null, string? details = null)
        {
            EventId = eventId;
            Details = details;
        }
    }
}