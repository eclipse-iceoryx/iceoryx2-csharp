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
    /// Represents an error caused by an invalid or corrupted native handle.
    /// Handles are opaque pointers to native iceoryx2 resources.
    /// </summary>
    /// <remarks>
    /// Common causes:
    /// <list type="bullet">
    /// <item>Using a handle after it has been disposed</item>
    /// <item>Handle corruption due to memory issues</item>
    /// <item>Passing an uninitialized handle</item>
    /// <item>Native resource was destroyed externally</item>
    /// </list>
    /// This error typically indicates a programming bug rather than a runtime condition.
    /// </remarks>
    public class InvalidHandleError : Iox2Error
    {
        /// <summary>
        /// Gets the type of handle that was invalid (e.g., "Publisher", "Subscriber"), if available.
        /// </summary>
        public string? HandleType { get; }

        /// <summary>
        /// Gets the error kind for pattern matching.
        /// </summary>
        public override Iox2ErrorKind Kind => Iox2ErrorKind.InvalidHandle;

        /// <summary>
        /// Gets additional details about the invalid handle.
        /// </summary>
        public override string? Details { get; }

        /// <summary>
        /// Gets a human-readable error message including handle type if available.
        /// </summary>
        public override string Message
        {
            get
            {
                var msg = HandleType != null
                    ? $"Invalid {HandleType} handle"
                    : "Invalid handle";
                return Details != null ? $"{msg}. Details: {Details}" : $"{msg}.";
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidHandleError"/> class.
        /// </summary>
        /// <param name="handleType">The type of handle that was invalid.</param>
        /// <param name="details">Optional details about the error.</param>
        public InvalidHandleError(string? handleType = null, string? details = null)
        {
            HandleType = handleType;
            Details = details;
        }
    }
}