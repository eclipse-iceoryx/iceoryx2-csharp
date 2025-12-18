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

using Iceoryx2.ErrorHandling;

namespace Iceoryx2;

/// <summary>
/// Error type for service discovery operations.
/// </summary>
public sealed class ServiceListError : Iox2Error
{
    private readonly ServiceListErrorKind _kind;
    private readonly string? _details;

    private ServiceListError(ServiceListErrorKind kind, string? details = null)
    {
        _kind = kind;
        _details = details;
    }

    /// <inheritdoc />
    public override string Message => _kind switch
    {
        ServiceListErrorKind.InsufficientPermissions => "Insufficient permissions to list services",
        ServiceListErrorKind.InternalError => "Internal error occurred while listing services",
        ServiceListErrorKind.Interrupt => "Service listing was interrupted",
        _ => "Unknown service list error"
    };

    /// <inheritdoc />
    public override Iox2ErrorKind Kind => Iox2ErrorKind.ServiceListFailed;

    /// <inheritdoc />
    public override string? Details => _details;

    /// <summary>
    /// Gets the specific kind of service list error.
    /// </summary>
    public ServiceListErrorKind ServiceListKind => _kind;

    /// <summary>
    /// Creates a ServiceListError for insufficient permissions.
    /// </summary>
    public static ServiceListError InsufficientPermissions => new(ServiceListErrorKind.InsufficientPermissions);

    /// <summary>
    /// Creates a ServiceListError for internal errors.
    /// </summary>
    public static ServiceListError InternalError => new(ServiceListErrorKind.InternalError);

    /// <summary>
    /// Creates a ServiceListError for interrupts.
    /// </summary>
    public static ServiceListError Interrupt => new(ServiceListErrorKind.Interrupt);
}

/// <summary>
/// Specific error kinds for service listing operations.
/// </summary>
public enum ServiceListErrorKind
{
    /// <summary>
    /// Insufficient permissions to access service directory.
    /// </summary>
    InsufficientPermissions,

    /// <summary>
    /// An internal error occurred during service discovery.
    /// </summary>
    InternalError,

    /// <summary>
    /// The service listing operation was interrupted.
    /// </summary>
    Interrupt
}