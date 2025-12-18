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
    /// Indicates an error that occurred during the process of loaning a sample in the Iceoryx2 framework.
    /// </summary>
    public class SampleLoanError : Iox2Error
    {
        /// <summary>
        /// Represents the specific kind of error encapsulated by the error instance.
        /// This property provides the error category as defined in the <c>Iox2ErrorKind</c> enumeration.
        /// It is overridden in derived error classes to specify the associated error kind relevant to them.
        /// </summary>
        public override Iox2ErrorKind Kind => Iox2ErrorKind.SampleLoanFailed;

        /// <summary>
        /// Provides additional details regarding the error, if available.
        /// </summary>
        public override string? Details { get; }

        /// <summary>
        /// Gets a message that describes the current error. The message provides an explanation
        /// for the error that occurred during an operation, along with additional details if available.
        /// </summary>
        public override string Message => Details != null
            ? $"Failed to loan sample. Details: {Details}"
            : "Failed to loan sample.";

        /// <summary>
        /// Represents an error that occurs when a sample loan operation fails.
        /// </summary>
        /// <remarks>
        /// This error is part of the Iceoryx2 error handling mechanism. A sample loan failure typically occurs
        /// when the system is unable to allocate or provide the requested sample.
        /// </remarks>
        /// <example>
        /// This class can be used to handle specific scenarios where the loaning of a sample fails
        /// and additional details may be provided through the <c>Details</c> property.
        /// </example>
        public SampleLoanError(string? details = null)
        {
            Details = details;
        }
    }
}