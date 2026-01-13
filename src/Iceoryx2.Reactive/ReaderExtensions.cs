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

using Iceoryx2.Blackboard;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Iceoryx2.Reactive;

/// <summary>
/// Provides extension methods to integrate iceoryx2 Blackboard Reader with Reactive Extensions (Rx).
/// Enables declarative, composable, and asynchronous data stream processing using IObservable&lt;T&gt;.
/// </summary>
public static class ReaderExtensions
{
    /// <summary>
    /// Converts a blackboard entry into an IObservable&lt;TValue&gt; stream that polls for value changes.
    /// This enables declarative Rx-style programming with LINQ operators (Where, Select, Buffer, etc.).
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="reader">The blackboard reader.</param>
    /// <param name="key">The key to observe.</param>
    /// <param name="pollingInterval">Optional polling interval (default: 10ms). Lower values reduce latency but increase CPU usage.</param>
    /// <param name="cancellationToken">Optional cancellation token to stop the observable stream.</param>
    /// <returns>An IObservable&lt;TValue&gt; that emits the current value on each poll.</returns>
    /// <example>
    /// <code>
    /// using var subscription = reader.AsObservable&lt;MyKey, MyValue&gt;(key)
    ///     .DistinctUntilChanged()
    ///     .Subscribe(value => Console.WriteLine($"Value changed: {value}"));
    /// </code>
    /// </example>
    public static IObservable<TValue> AsObservable<TKey, TValue>(
        this Reader<TKey> reader,
        TKey key,
        TimeSpan? pollingInterval = null,
        CancellationToken cancellationToken = default)
        where TKey : unmanaged
        where TValue : unmanaged
    {
        if (reader == null)
            throw new ArgumentNullException(nameof(reader));

        var interval = pollingInterval ?? TimeSpan.FromMilliseconds(10);

        return new ReaderObservable<TKey, TValue>(reader, key, interval, cancellationToken);
    }

    /// <summary>
    /// Converts a blackboard entry into an async enumerable stream (IAsyncEnumerable&lt;TValue&gt;).
    /// This enables use with C# 8.0+ async streams and await foreach.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="reader">The blackboard reader.</param>
    /// <param name="key">The key to observe.</param>
    /// <param name="pollingInterval">Optional polling interval (default: 10ms).</param>
    /// <param name="cancellationToken">Optional cancellation token to stop the stream.</param>
    /// <returns>An IAsyncEnumerable&lt;TValue&gt; that yields the current value on each poll.</returns>
    /// <example>
    /// <code>
    /// await foreach (var value in reader.AsAsyncEnumerable&lt;MyKey, MyValue&gt;(key, token))
    /// {
    ///     Console.WriteLine($"Current value: {value}");
    /// }
    /// </code>
    /// </example>
    public static async IAsyncEnumerable<TValue> AsAsyncEnumerable<TKey, TValue>(
        this Reader<TKey> reader,
        TKey key,
        TimeSpan? pollingInterval = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TKey : unmanaged
        where TValue : unmanaged
    {
        if (reader == null)
            throw new ArgumentNullException(nameof(reader));

        var interval = pollingInterval ?? TimeSpan.FromMilliseconds(10);

        while (!cancellationToken.IsCancellationRequested)
        {
            var entryResult = reader.Entry<TValue>(key);

            if (entryResult.IsOk)
            {
                using var entry = entryResult.Unwrap();
                var valueResult = entry.Get();

                if (valueResult.IsOk)
                {
                    yield return valueResult.Unwrap();
                }
            }

            await Task.Delay(interval, cancellationToken);
        }
    }

    /// <summary>
    /// Converts a blackboard entry into an async enumerable stream that only yields when the value changes.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="reader">The blackboard reader.</param>
    /// <param name="key">The key to observe.</param>
    /// <param name="equalityComparer">Optional equality comparer for detecting changes.</param>
    /// <param name="pollingInterval">Optional polling interval (default: 10ms).</param>
    /// <param name="cancellationToken">Optional cancellation token to stop the stream.</param>
    /// <returns>An IAsyncEnumerable&lt;TValue&gt; that yields only when the value changes.</returns>
    public static async IAsyncEnumerable<TValue> AsDistinctAsyncEnumerable<TKey, TValue>(
        this Reader<TKey> reader,
        TKey key,
        IEqualityComparer<TValue>? equalityComparer = null,
        TimeSpan? pollingInterval = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TKey : unmanaged
        where TValue : unmanaged
    {
        if (reader == null)
            throw new ArgumentNullException(nameof(reader));

        var comparer = equalityComparer ?? EqualityComparer<TValue>.Default;
        var interval = pollingInterval ?? TimeSpan.FromMilliseconds(10);
        var hasLastValue = false;
        TValue lastValue = default;

        while (!cancellationToken.IsCancellationRequested)
        {
            var entryResult = reader.Entry<TValue>(key);

            if (entryResult.IsOk)
            {
                using var entry = entryResult.Unwrap();
                var valueResult = entry.Get();

                if (valueResult.IsOk)
                {
                    var currentValue = valueResult.Unwrap();

                    if (!hasLastValue || !comparer.Equals(lastValue, currentValue))
                    {
                        lastValue = currentValue;
                        hasLastValue = true;
                        yield return currentValue;
                    }
                }
            }

            await Task.Delay(interval, cancellationToken);
        }
    }
}