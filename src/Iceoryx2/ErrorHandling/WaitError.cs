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
    /// Represents an error that occurred during a wait operation.
    /// Wait operations block until an event notification is received.
    /// </summary>
    /// <remarks>
    /// Common causes:
    /// <list type="bullet">
    /// <item>Listener is in invalid state</item>
    /// <item>Wait was interrupted by signal</item>
    /// <item>System resource error</item>
    /// <item>Timeout occurred (if timeout-based wait)</item>
    /// </list>
    /// </remarks>
    public class WaitError : Iox2Error
    {
        /// <summary>
        /// Gets the error kind for pattern matching.
        /// </summary>
        public override Iox2ErrorKind Kind => Iox2ErrorKind.WaitFailed;

        /// <summary>
        /// Gets additional details about why the wait operation failed.
        /// </summary>
        public override string? Details { get; }

        /// <summary>
        /// Gets a human-readable error message.
        /// </summary>
        public override string Message => Details != null
            ? $"Failed to wait for event. Details: {Details}"
            : "Failed to wait for event.";

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitError"/> class.
        /// </summary>
        /// <param name="details">Optional details about the error.</param>
        public WaitError(string? details = null)
        {
            Details = details;
        }
    }
}