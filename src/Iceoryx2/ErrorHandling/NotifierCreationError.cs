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
    /// Represents an error that occurred during notifier creation.
    /// Notifiers send event notifications to listeners.
    /// </summary>
    /// <remarks>
    /// Common causes:
    /// <list type="bullet">
    /// <item>Maximum number of notifiers reached</item>
    /// <item>Event service does not exist</item>
    /// <item>Insufficient resources</item>
    /// </list>
    /// </remarks>
    public class NotifierCreationError : Iox2Error
    {
        /// <summary>
        /// Gets the error kind for pattern matching.
        /// </summary>
        public override Iox2ErrorKind Kind => Iox2ErrorKind.NotifierCreationFailed;

        /// <summary>
        /// Gets additional details about why notifier creation failed.
        /// </summary>
        public override string? Details { get; }

        /// <summary>
        /// Gets a human-readable error message.
        /// </summary>
        public override string Message => Details != null
            ? $"Failed to create notifier. Details: {Details}"
            : "Failed to create notifier.";

        /// <summary>
        /// Initializes a new instance of the <see cref="NotifierCreationError"/> class.
        /// </summary>
        /// <param name="details">Optional details about the error.</param>
        public NotifierCreationError(string? details = null)
        {
            Details = details;
        }
    }
}