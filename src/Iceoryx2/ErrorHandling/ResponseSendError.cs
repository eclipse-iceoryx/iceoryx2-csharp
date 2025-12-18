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
    /// Represents an error that occurred when sending a response back to a client.
    /// After loaning and writing a response, servers send it back to the requesting client.
    /// </summary>
    /// <remarks>
    /// Common causes:
    /// <list type="bullet">
    /// <item>Client disconnected or no longer exists</item>
    /// <item>Client response queue is full</item>
    /// <item>Communication channel corrupted</item>
    /// </list>
    /// </remarks>
    public class ResponseSendError : Iox2Error
    {
        /// <summary>
        /// Gets the error kind for pattern matching.
        /// </summary>
        public override Iox2ErrorKind Kind => Iox2ErrorKind.ResponseSendFailed;

        /// <summary>
        /// Gets additional details about why the response send failed.
        /// </summary>
        public override string? Details { get; }

        /// <summary>
        /// Gets a human-readable error message.
        /// </summary>
        public override string Message => Details != null
            ? $"Failed to send response. Details: {Details}"
            : "Failed to send response.";

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseSendError"/> class.
        /// </summary>
        /// <param name="details">Optional details about the error.</param>
        public ResponseSendError(string? details = null)
        {
            Details = details;
        }
    }
}