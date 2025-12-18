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

using System;
using static Iceoryx2.Native.Iox2NativeMethods;

namespace Iceoryx2.RequestResponse;

/// <summary>
/// Delegate for open or create operations.
/// </summary>
internal delegate int OpenOrCreateDelegate(IntPtr handle, IntPtr structPtr, out IntPtr outHandle);

/// <summary>
/// Builder for creating or opening request-response services.
/// </summary>
/// <typeparam name="TRequest">The type of the request payload.</typeparam>
/// <typeparam name="TResponse">The type of the response payload.</typeparam>
public sealed class RequestResponseServiceBuilder<TRequest, TResponse>
    where TRequest : unmanaged
    where TResponse : unmanaged
{
    private readonly Node _node;

    internal RequestResponseServiceBuilder(Node node)
    {
        _node = node;
    }

    /// <summary>
    /// Opens an existing request-response service or creates a new one if it doesn't exist.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <returns>A Result containing the request-response service or an error.</returns>
    public Result<RequestResponseService<TRequest, TResponse>, Iox2Error> Open(string serviceName)
    {
        return OpenOrCreate(serviceName, iox2_service_builder_request_response_open_or_create);
    }

    /// <summary>
    /// Creates a new request-response service. Fails if the service already exists.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <returns>A Result containing the request-response service or an error.</returns>
    public Result<RequestResponseService<TRequest, TResponse>, Iox2Error> Create(string serviceName)
    {
        return OpenOrCreate(serviceName, iox2_service_builder_request_response_create);
    }

    private unsafe Result<RequestResponseService<TRequest, TResponse>, Iox2Error> OpenOrCreate(
        string serviceName,
        OpenOrCreateDelegate openOrCreateFunc)
    {
        // Create service name
        var serviceNameResult = iox2_service_name_new(
            IntPtr.Zero,
            serviceName,
            serviceName.Length,
            out var serviceNameHandle);

        if (serviceNameResult != IOX2_OK)
        {
            return Result<RequestResponseService<TRequest, TResponse>, Iox2Error>.Err(Iox2Error.RequestResponseServiceCreationFailed);
        }

        try
        {
            var serviceNamePtr = iox2_cast_service_name_ptr(serviceNameHandle);

            // Get service builder
            var nodeHandle = _node._handle.DangerousGetHandle();
            var serviceBuilderHandle = iox2_node_service_builder(
                ref nodeHandle,
                IntPtr.Zero,
                serviceNamePtr);

            if (serviceBuilderHandle == IntPtr.Zero)
            {
                return Result<RequestResponseService<TRequest, TResponse>, Iox2Error>.Err(Iox2Error.RequestResponseServiceCreationFailed);
            }

            // Get request-response builder
            var requestResponseBuilderHandle = iox2_service_builder_request_response(serviceBuilderHandle);

            if (requestResponseBuilderHandle == IntPtr.Zero)
            {
                return Result<RequestResponseService<TRequest, TResponse>, Iox2Error>.Err(Iox2Error.RequestResponseServiceCreationFailed);
            }

            // Set request payload type details
            var requestTypeName = ServiceBuilder.GetRustCompatibleTypeName<TRequest>();
            var requestTypeSize = (ulong)sizeof(TRequest);
            var requestTypeAlignment = GetAlignment<TRequest>(requestTypeSize);

            var requestResult = iox2_service_builder_request_response_set_request_payload_type_details(
                ref requestResponseBuilderHandle,
                iox2_type_variant_e.FIXED_SIZE,
                requestTypeName,
                requestTypeName.Length,
                requestTypeSize,
                requestTypeAlignment);

            if (requestResult != IOX2_OK)
            {
                return Result<RequestResponseService<TRequest, TResponse>, Iox2Error>.Err(Iox2Error.RequestResponseServiceCreationFailed);
            }

            // Set response payload type details
            var responseTypeName = ServiceBuilder.GetRustCompatibleTypeName<TResponse>();
            var responseTypeSize = (ulong)sizeof(TResponse);
            var responseTypeAlignment = GetAlignment<TResponse>(responseTypeSize);

            var responseResult = iox2_service_builder_request_response_set_response_payload_type_details(
                ref requestResponseBuilderHandle,
                iox2_type_variant_e.FIXED_SIZE,
                responseTypeName,
                responseTypeName.Length,
                responseTypeSize,
                responseTypeAlignment);

            if (responseResult != IOX2_OK)
            {
                return Result<RequestResponseService<TRequest, TResponse>, Iox2Error>.Err(Iox2Error.RequestResponseServiceCreationFailed);
            }

            // Open or create the service
            var result = openOrCreateFunc(
                requestResponseBuilderHandle,
                IntPtr.Zero,
                out var portFactoryHandle);

            if (result != IOX2_OK)
            {
                return Result<RequestResponseService<TRequest, TResponse>, Iox2Error>.Err(Iox2Error.RequestResponseServiceCreationFailed);
            }

            return Result<RequestResponseService<TRequest, TResponse>, Iox2Error>.Ok(
                new RequestResponseService<TRequest, TResponse>(portFactoryHandle));
        }
        finally
        {
            iox2_service_name_drop(serviceNameHandle);
        }
    }

    private static ulong GetAlignment<T>(ulong typeSize) where T : unmanaged
    {
        if (typeof(T).IsPrimitive)
        {
            return typeSize;
        }
        else
        {
            // For structs, check if there's a StructLayout attribute specifying Pack
            var layoutAttr = typeof(T).StructLayoutAttribute;
            if (layoutAttr != null && layoutAttr.Pack > 0)
            {
                return (ulong)layoutAttr.Pack;
            }
            else
            {
                // Default to pointer size for alignment
                return (ulong)IntPtr.Size;
            }
        }
    }
}