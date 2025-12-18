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
    /// Represents an error that occurred during subscriber creation.
    /// Subscribers receive data samples from publishers via shared memory.
    /// </summary>
    /// <remarks>
    /// Common causes:
    /// <list type="bullet">
    /// <item>Maximum number of subscribers for the service reached</item>
    /// <item>Insufficient shared memory for subscriber metadata</item>
    /// <item>Service does not exist or has incompatible type</item>
    /// <item>Invalid buffer size or subscriber configuration</item>
    /// </list>
    /// </remarks>
    public class SubscriberCreationError : Iox2Error
    {
        /// <summary>
        /// Gets the error kind for pattern matching.
        /// </summary>
        public override Iox2ErrorKind Kind => Iox2ErrorKind.SubscriberCreationFailed;

        /// <summary>
        /// Gets additional details about why subscriber creation failed.
        /// </summary>
        public override string? Details { get; }

        /// <summary>
        /// Gets a human-readable error message.
        /// </summary>
        public override string Message => Details != null
            ? $"Failed to create subscriber. Details: {Details}"
            : "Failed to create subscriber.";

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriberCreationError"/> class.
        /// </summary>
        /// <param name="details">Optional details about the error.</param>
        public SubscriberCreationError(string? details = null)
        {
            Details = details;
        }
    }
}