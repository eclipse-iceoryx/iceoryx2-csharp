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
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Iceoryx2.Reactive;

/// <summary>
/// Provides extension methods to integrate iceoryx2 Subscriber with Reactive Extensions (Rx).
/// Enables declarative, composable, and asynchronous data stream processing using IObservable&lt;T&gt;.
/// </summary>
public static class SubscriberExtensions
{
    /// <summary>
    /// Converts an iceoryx2 Subscriber into an IObservable&lt;T&gt; stream.
    /// This enables declarative Rx-style programming with LINQ operators (Where, Select, Buffer, etc.).
    /// </summary>
    /// <typeparam name="T">The unmanaged type of data being received (must match the service type)</typeparam>
    /// <param name="subscriber">The iceoryx2 subscriber to observe</param>
    /// <param name="pollingInterval">Optional polling interval (default: 10ms). Lower values reduce latency but increase CPU usage.</param>
    /// <param name="cancellationToken">Optional cancellation token to stop the observable stream</param>
    /// <returns>An IObservable&lt;T&gt; that emits received samples</returns>
    /// <example>
    /// <code>
    /// using var subscription = subscriber.AsObservable&lt;MyData&gt;()
    ///     .Where(data => data.IsValid)
    ///     .Subscribe(data => Console.WriteLine($"Received: {data}"));
    /// </code>
    /// </example>
    public static IObservable<T> AsObservable<T>(
        this Subscriber subscriber,
        TimeSpan? pollingInterval = null,
        CancellationToken cancellationToken = default) where T : unmanaged
    {
        if (subscriber == null)
            throw new ArgumentNullException(nameof(subscriber));

        var interval = pollingInterval ?? TimeSpan.FromMilliseconds(10);

        return new SubscriberObservable<T>(subscriber, interval, cancellationToken);
    }

    /// <summary>
    /// Converts an iceoryx2 Subscriber into an async enumerable stream (IAsyncEnumerable&lt;T&gt;).
    /// This enables use with C# 8.0+ async streams and await foreach.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of data being received</typeparam>
    /// <param name="subscriber">The iceoryx2 subscriber to observe</param>
    /// <param name="pollingInterval">Optional polling interval (default: 10ms)</param>
    /// <param name="cancellationToken">Optional cancellation token to stop the stream</param>
    /// <returns>An IAsyncEnumerable&lt;T&gt; that yields received samples</returns>
    /// <example>
    /// <code>
    /// await foreach (var data in subscriber.AsAsyncEnumerable&lt;MyData&gt;(token))
    /// {
    ///     Console.WriteLine($"Received: {data}");
    /// }
    /// </code>
    /// </example>
    public static async IAsyncEnumerable<T> AsAsyncEnumerable<T>(
        this Subscriber subscriber,
        TimeSpan? pollingInterval = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default) where T : unmanaged
    {
        if (subscriber == null)
            throw new ArgumentNullException(nameof(subscriber));

        var interval = pollingInterval ?? TimeSpan.FromMilliseconds(10);

        while (!cancellationToken.IsCancellationRequested)
        {
            var result = subscriber.Receive<T>();

            if (result.IsOk)
            {
                var sample = result.Unwrap();
                if (sample != null)
                {
                    yield return sample.Payload;
                    sample.Dispose();
                }
            }

            await Task.Delay(interval, cancellationToken);
        }
    }
}