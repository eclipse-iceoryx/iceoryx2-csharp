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
    /// Indicates an error that occurs when updating publisher-subscriber connections fails.
    /// </summary>
    public class ConnectionUpdateError : Iox2Error
    {
        /// <summary>
        /// Represents the kind of error associated with the specific implementation of the <see cref="Iox2Error"/> class.
        /// Provides an enumeration value of type <see cref="Iox2ErrorKind"/> that defines the nature of the error.
        /// </summary>
        public override Iox2ErrorKind Kind => Iox2ErrorKind.ConnectionUpdateFailed;

        /// <summary>
        /// Provides additional details about the error, if available.
        /// </summary>
        /// <remarks>
        /// This property may contain supplementary information regarding the specific error encountered
        /// during an operation. If no details are specified, this property may be null.
        /// </remarks>
        public override string? Details { get; }

        /// <summary>
        /// Gets a descriptive error message for the specific error instance.
        /// This message provides details about the error, which may include
        /// additional information depending on the error type and its context.
        /// </summary>
        public override string Message => Details != null
            ? $"Failed to update connections. Details: {Details}"
            : "Failed to update connections.";

        /// <summary>
        /// Represents an error that occurs during a connection update operation.
        /// </summary>
        /// <remarks>
        /// This error is specifically used to indicate that updating connections between 
        /// publishers and subscribers has failed. Connection updates are critical for
        /// delivering history to late-joining subscribers.
        /// </remarks>
        public ConnectionUpdateError(string? details = null)
        {
            Details = details;
        }
    }
}