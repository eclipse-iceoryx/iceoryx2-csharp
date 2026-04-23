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
using Iceoryx2.Native;
using Xunit.Sdk;

namespace Iceoryx2.Tests.Ffi;

/// <summary>
/// xUnit assertions over raw FFI return codes, producing messages that include
/// the decoded enum name when possible.
/// </summary>
internal static class FfiAssert
{
    /// <summary>
    /// Asserts that <paramref name="result"/> equals <see cref="Iox2NativeMethods.IOX2_OK"/>.
    /// </summary>
    public static void Ok(int result, string operation)
    {
        if (result != Iox2NativeMethods.IOX2_OK)
        {
            throw new XunitException(
                $"FFI call '{operation}' failed: code={result}");
        }
    }

    /// <summary>
    /// Asserts that <paramref name="result"/> equals <see cref="Iox2NativeMethods.IOX2_OK"/>
    /// and includes a caller-supplied decoded error name on failure.
    /// </summary>
    public static void Ok(int result, string operation, Func<int, string> decode)
    {
        if (result != Iox2NativeMethods.IOX2_OK)
        {
            throw new XunitException(
                $"FFI call '{operation}' failed: code={result} ({decode(result)})");
        }
    }
}
