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

namespace Iceoryx2;

/// <summary>
/// Custom attribute to specify the Rust/C type name for cross-language communication.
/// This allows C# types to be mapped to specific type names in Rust or C, enabling
/// seamless interoperability when the same data structure is defined in multiple languages.
/// </summary>
/// <remarks>
/// When a C# struct needs to communicate with Rust or C code, the type name must match
/// across language boundaries. By default, C# uses the type's name (e.g., "TransmissionData"),
/// but this attribute allows you to specify a different name if needed.
/// 
/// Example:
/// <code>
/// [StructLayout(LayoutKind.Sequential)]
/// [Iox2Type("TransmissionData")]
/// public struct TransmissionData
/// {
///     public int X;
///     public int Y;
///     public double Funky;
/// }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class Iox2TypeAttribute : Attribute
{
    /// <summary>
    /// Gets the type name to use for cross-language type identification.
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Iox2TypeAttribute"/> class.
    /// </summary>
    /// <param name="typeName">The type name to use for cross-language communication.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="typeName"/> is null.</exception>
    public Iox2TypeAttribute(string typeName)
    {
        TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
    }
}