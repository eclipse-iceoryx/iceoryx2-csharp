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
    /// Denotes an error that arises when a client could not be successfully created.
    /// </summary>
    public class ClientCreationError : Iox2Error
    {
        /// <summary>
        /// Gets the kind of error represented by this instance. Provides a classification from the <c>Iox2ErrorKind</c> enumeration
        /// that indicates the specific type of error, such as client creation failure.
        /// </summary>
        public override Iox2ErrorKind Kind => Iox2ErrorKind.ClientCreationFailed;

        /// <summary>
        /// Provides additional details about the error that occurred.
        /// </summary>
        /// <remarks>
        /// This property may contain specific information describing the nature of the error,
        /// which can assist in debugging or logging purposes. If no specific details are available,
        /// the property value may be null.
        /// </remarks>
        public override string? Details { get; }

        /// <summary>
        /// Gets the error message associated with this instance of the error.
        /// </summary>
        /// <remarks>
        /// The message provides a description of the error that occurred.
        /// For instance, in the case of a <c>ClientCreationError</c>, the message will
        /// specify that the client creation has failed, optionally including additional
        /// details if available.
        /// </remarks>
        public override string Message => Details != null
            ? $"Failed to create client. Details: {Details}"
            : "Failed to create client.";

        /// <summary>
        /// Represents an error that occurs during the creation of a client in the Iceoryx2 library.
        /// </summary>
        /// <remarks>
        /// This error is returned when client creation fails, providing additional details if available.
        /// </remarks>
        public ClientCreationError(string? details = null)
        {
            Details = details;
        }
    }
}