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

using Iceoryx2.SafeHandles;
using System;

namespace Iceoryx2;

/// <summary>
/// Builder for creating publishers with custom Quality of Service (QoS) settings.
/// </summary>
public sealed class PublisherBuilder
{
    private readonly Service _service;
    private ulong? _maxLoanedSamples;

    internal PublisherBuilder(Service service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    /// <summary>
    /// Sets the maximum number of samples a publisher can loan simultaneously.
    /// This defines how many samples can be in-flight (loaned but not yet sent) at the same time.
    /// </summary>
    /// <param name="value">Maximum loaned samples (default: 2)</param>
    /// <returns>This builder for method chaining</returns>
    public PublisherBuilder MaxLoanedSamples(ulong value)
    {
        _maxLoanedSamples = value;
        return this;
    }

    /// <summary>
    /// Creates the publisher with the configured QoS settings.
    /// </summary>
    /// <returns>Result containing the created Publisher or an error</returns>
    public Result<Publisher, Iox2Error> Create()
    {
        try
        {
            // Create publisher builder - pass by reference for handle
            var portFactoryHandle = _service.GetHandle().DangerousGetHandle();
            var publisherBuilderHandle = Native.Iox2NativeMethods.iox2_port_factory_pub_sub_publisher_builder(
                ref portFactoryHandle,  // Pass by reference - C expects pointer to handle
                IntPtr.Zero);  // NULL - let C allocate the struct

            if (publisherBuilderHandle == IntPtr.Zero)
                return Result<Publisher, Iox2Error>.Err(Iox2Error.PublisherCreationFailed);

            // Apply QoS settings if specified
            if (_maxLoanedSamples.HasValue)
            {
                Native.Iox2NativeMethods.iox2_port_factory_publisher_builder_set_max_loaned_samples(
                    ref publisherBuilderHandle, new UIntPtr(_maxLoanedSamples.Value));
            }

            // Create publisher - pass NULL to let C allocate on heap
            var result = Native.Iox2NativeMethods.iox2_port_factory_publisher_builder_create(
                publisherBuilderHandle,
                IntPtr.Zero,  // NULL - let C allocate the struct
                out var publisherHandle);

            if (result != Native.Iox2NativeMethods.IOX2_OK || publisherHandle == IntPtr.Zero)
                return Result<Publisher, Iox2Error>.Err(Iox2Error.PublisherCreationFailed);

            var handle = new SafePublisherHandle(publisherHandle);
            var publisher = new Publisher(handle);

            return Result<Publisher, Iox2Error>.Ok(publisher);
        }
        catch (Exception)
        {
            return Result<Publisher, Iox2Error>.Err(Iox2Error.PublisherCreationFailed);
        }
    }
}