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
/// Represents a result type that can either contain a success value or an error.
/// Similar to Rust's Result type.
/// </summary>
/// <typeparam name="T">The type of the success value</typeparam>
/// <typeparam name="E">The type of the error value</typeparam>
public readonly struct Result<T, E>
{
    private readonly bool _isOk;
    private readonly T? _value;
    private readonly E? _error;

    private Result(T value)
    {
        _isOk = true;
        _value = value;
        _error = default;
    }

    private Result(E error)
    {
        _isOk = false;
        _value = default;
        _error = error;
    }

    /// <summary>
    /// Creates a success result.
    /// </summary>
    public static Result<T, E> Ok(T value) => new(value);

    /// <summary>
    /// Creates an error result.
    /// </summary>
    public static Result<T, E> Err(E error) => new(error);

    /// <summary>
    /// Returns true if the result is Ok.
    /// </summary>
    public bool IsOk => _isOk;

    /// <summary>
    /// Returns true if the result is Err.
    /// </summary>
    public bool IsErr => !_isOk;

    /// <summary>
    /// Unwraps the success value, throwing an exception if the result is an error.
    /// </summary>
    public T Expect(string message)
    {
        if (_isOk)
            return _value!;

        throw new InvalidOperationException($"{message}: {_error}");
    }

    /// <summary>
    /// Unwraps the success value, throwing an exception if the result is an error.
    /// </summary>
    public T Unwrap()
    {
        if (_isOk)
            return _value!;

        throw new InvalidOperationException($"Called Unwrap on an error result: {_error}");
    }

    /// <summary>
    /// Returns the success value or a default value if the result is an error.
    /// </summary>
    public T UnwrapOr(T defaultValue) => _isOk ? _value! : defaultValue;

    /// <summary>
    /// Matches on the result, executing the appropriate function.
    /// </summary>
    public TResult Match<TResult>(Func<T, TResult> onOk, Func<E, TResult> onErr)
    {
        return _isOk ? onOk(_value!) : onErr(_error!);
    }

    /// <summary>
    /// Maps the success value to a new value.
    /// </summary>
    public Result<TNew, E> Map<TNew>(Func<T, TNew> mapper)
    {
        return _isOk ? Result<TNew, E>.Ok(mapper(_value!)) : Result<TNew, E>.Err(_error!);
    }

    /// <summary>
    /// Maps the error value to a new error.
    /// </summary>
    public Result<T, ENew> MapErr<ENew>(Func<E, ENew> mapper)
    {
        return _isOk ? Result<T, ENew>.Ok(_value!) : Result<T, ENew>.Err(mapper(_error!));
    }

    /// <summary>
    /// Returns a string representation of the result, indicating whether it is a success or an error.
    /// </summary>
    /// <returns>
    /// A string in the format "Ok(value)" if the result is a success, or "Err(error)" if the result is an error.
    /// </returns>
    public override string ToString()
    {
        return _isOk ? $"Ok({_value})" : $"Err({_error})";
    }
}