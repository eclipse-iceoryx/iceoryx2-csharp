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
    /// Represents an error that occurred during WaitSet attachment.
    /// Listeners, notifiers, and other event sources can be attached to a WaitSet for multiplexing.
    /// </summary>
    /// <remarks>
    /// Common causes:
    /// <list type="bullet">
    /// <item>WaitSet is at maximum capacity</item>
    /// <item>Event source is already attached</item>
    /// <item>Event source is in invalid state</item>
    /// <item>WaitSet is in invalid state</item>
    /// </list>
    /// </remarks>
    public class WaitSetAttachmentError : Iox2Error
    {
        /// <summary>
        /// Gets the error kind for pattern matching.
        /// </summary>
        public override Iox2ErrorKind Kind => Iox2ErrorKind.WaitSetAttachmentFailed;

        /// <summary>
        /// Gets additional details about why the attachment failed.
        /// </summary>
        public override string? Details { get; }

        /// <summary>
        /// Gets a human-readable error message.
        /// </summary>
        public override string Message => Details != null
            ? $"Failed to attach to WaitSet. Details: {Details}"
            : "Failed to attach to WaitSet.";

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitSetAttachmentError"/> class.
        /// </summary>
        /// <param name="details">Optional details about the error.</param>
        public WaitSetAttachmentError(string? details = null)
        {
            Details = details;
        }
    }
}