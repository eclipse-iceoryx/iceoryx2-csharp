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
    /// Represents an error that occurred when loaning a request buffer.
    /// In request-response services, clients loan request buffers to send requests to servers.
    /// </summary>
    /// <remarks>
    /// Common causes:
    /// <list type="bullet">
    /// <item>Out of request buffers (all buffers in use)</item>
    /// <item>Insufficient shared memory</item>
    /// <item>Client is in invalid state</item>
    /// </list>
    /// </remarks>
    public class RequestLoanError : Iox2Error
    {
        /// <summary>
        /// Gets the error kind for pattern matching.
        /// </summary>
        public override Iox2ErrorKind Kind => Iox2ErrorKind.RequestLoanFailed;

        /// <summary>
        /// Gets additional details about why the request loan failed.
        /// </summary>
        public override string? Details { get; }

        /// <summary>
        /// Gets a human-readable error message.
        /// </summary>
        public override string Message => Details != null
            ? $"Failed to loan request. Details: {Details}"
            : "Failed to loan request.";

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestLoanError"/> class.
        /// </summary>
        /// <param name="details">Optional details about the error.</param>
        public RequestLoanError(string? details = null)
        {
            Details = details;
        }
    }
}