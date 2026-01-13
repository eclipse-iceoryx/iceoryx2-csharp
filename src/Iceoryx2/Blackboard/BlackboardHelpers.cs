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

namespace Iceoryx2.Blackboard;

/// <summary>
/// Provides helper methods for blackboard operations.
/// </summary>
internal static class BlackboardHelpers
{
    /// <summary>
    /// Gets the alignment for a given type.
    /// For primitive types, returns the type size.
    /// For struct types, returns the Pack value from StructLayoutAttribute if specified, otherwise IntPtr.Size.
    /// </summary>
    /// <typeparam name="T">The type to get alignment for (must be unmanaged).</typeparam>
    /// <param name="typeSize">The size of the type in bytes.</param>
    /// <returns>The alignment value in bytes.</returns>
    public static ulong GetAlignment<T>(ulong typeSize) where T : unmanaged
    {
        if (typeof(T).IsPrimitive)
        {
            return typeSize;
        }
        else
        {
            var layoutAttr = typeof(T).StructLayoutAttribute;
            if (layoutAttr != null && layoutAttr.Pack > 0)
            {
                return (ulong)layoutAttr.Pack;
            }
            else
            {
                return (ulong)IntPtr.Size;
            }
        }
    }
}
