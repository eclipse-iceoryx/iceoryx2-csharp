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

namespace Iceoryx2;

/// <summary>
/// Represents a unit type commonly used to indicate the absence of a meaningful value.
/// </summary>
public readonly struct Unit
{
    /// <summary>
    /// Represents the singleton instance of the <see cref="Unit"/> struct.
    /// It is used to signify the absence of a meaningful value in operations
    /// that return a result without a tangible return value, commonly used in
    /// conjunction with the <see cref="Result{T, E}"/> struct.
    /// </summary>
    public static readonly Unit Value = new();
}