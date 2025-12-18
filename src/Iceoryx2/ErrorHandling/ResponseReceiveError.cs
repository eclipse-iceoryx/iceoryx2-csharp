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
    /// Represents an error that occurred when receiving a response from a server.
    /// Clients receive responses after sending requests to servers.
    /// </summary>
    /// <remarks>
    /// Common causes:
    /// <list type="bullet">
    /// <item>Client is in invalid state</item>
    /// <item>Communication channel corrupted</item>
    /// <item>System resource error</item>
    /// </list>
    /// Note: No response available (timeout) is not an error and returns None/null.
    /// </remarks>
    public class ResponseReceiveError : Iox2Error
    {
        /// <summary>
        /// Gets the error kind for pattern matching.
        /// </summary>
        public override Iox2ErrorKind Kind => Iox2ErrorKind.ResponseReceiveFailed;

        /// <summary>
        /// Gets additional details about why the response receive failed.
        /// </summary>
        public override string? Details { get; }

        /// <summary>
        /// Gets a human-readable error message.
        /// </summary>
        public override string Message => Details != null
            ? $"Failed to receive response. Details: {Details}"
            : "Failed to receive response.";

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseReceiveError"/> class.
        /// </summary>
        /// <param name="details">Optional details about the error.</param>
        public ResponseReceiveError(string? details = null)
        {
            Details = details;
        }
    }
}