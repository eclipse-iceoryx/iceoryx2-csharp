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
/// Extension methods for Result types.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts a nullable reference to a Result.
    /// </summary>
    public static Result<T, E> ToResult<T, E>(this T? value, E error) where T : class
    {
        return value != null ? Result<T, E>.Ok(value) : Result<T, E>.Err(error);
    }
}