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
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Iceoryx2.Reactive;

/// <summary>
/// Provides extension methods to integrate iceoryx2 Listener with Reactive Extensions (Rx).
/// Listeners provide truly event-driven notifications using WaitSet with platform-specific mechanisms (epoll/kqueue).
/// </summary>
public static class ListenerExtensions
{
    /// <summary>
    /// Converts an iceoryx2 Listener into an IObservable&lt;EventId&gt; stream using WaitSet.
    /// This is truly event-driven using platform-specific mechanisms (epoll/kqueue), providing
    /// low latency and low CPU usage compared to polling-based approaches.
    /// </summary>
    /// <param name="listener">The iceoryx2 listener to observe</param>
    /// <param name="deadline">Optional deadline for receiving events</param>
    /// <param name="cancellationToken">Optional cancellation token to stop the observable stream</param>
    /// <returns>An IObservable&lt;EventId&gt; that emits received event IDs using event-driven WaitSet</returns>
    /// <example>
    /// <code>
    /// using var subscription = listener.AsObservable(
    ///     deadline: TimeSpan.FromSeconds(1))
    ///     .Subscribe(eventId => Console.WriteLine($"Event: {eventId.Value}"));
    /// </code>
    /// </example>
    public static IObservable<EventId> AsObservable(
        this Listener listener,
        TimeSpan? deadline = null,
        CancellationToken cancellationToken = default)
    {
        if (listener == null)
            throw new ArgumentNullException(nameof(listener));

        return Observable.Create<EventId>(observer =>
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var task = Task.Run(() =>
            {
                using var waitset = WaitSetBuilder.New()
                    .Create()
                    .Expect("Failed to create WaitSet");

                using var guard = deadline.HasValue
                    ? waitset.AttachDeadline(listener, deadline.Value).Expect("Failed to attach listener with deadline")
                    : waitset.AttachNotification(listener).Expect("Failed to attach listener");

                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        var waitResult = waitset.WaitAndProcess((attachmentId) =>
                        {
                            if (attachmentId.HasEventFrom(guard))
                            {
                                if (deadline.HasValue && attachmentId.HasMissedDeadline(guard))
                                {
                                    // Deadline missed - continue waiting
                                    return CallbackProgression.Continue;
                                }

                                // Event available - consume all pending events
                                while (true)
                                {
                                    var eventResult = listener.TryWait();
                                    if (eventResult.IsOk)
                                    {
                                        var eventIdOpt = eventResult.Unwrap();
                                        if (eventIdOpt.HasValue)
                                        {
                                            observer.OnNext(eventIdOpt.Value);
                                        }
                                        else
                                        {
                                            break; // No more events
                                        }
                                    }
                                    else
                                    {
                                        observer.OnError(new InvalidOperationException("Failed to receive event"));
                                        return CallbackProgression.Stop;
                                    }
                                }
                            }
                            return CallbackProgression.Continue;
                        });

                        if (!waitResult.IsOk)
                        {
                            observer.OnError(new InvalidOperationException("WaitSet failed"));
                            break;
                        }
                    }
                    observer.OnCompleted();
                }
                catch (OperationCanceledException)
                {
                    observer.OnCompleted();
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }
            }, cts.Token);

            return Disposable.Create(() =>
            {
                cts.Cancel();
                try
                {
                    task.Wait(TimeSpan.FromSeconds(1));
                }
                catch (AggregateException)
                {
                    // Expected when cancelled
                }
                cts.Dispose();
            });
        });
    }

    /// <summary>
    /// Converts an iceoryx2 Listener into an async enumerable stream using WaitSet.
    /// This is truly event-driven using platform-specific mechanisms (epoll/kqueue).
    /// </summary>
    /// <param name="listener">The iceoryx2 listener to observe</param>
    /// <param name="deadline">Optional deadline for receiving events</param>
    /// <param name="cancellationToken">Optional cancellation token to stop the stream</param>
    /// <returns>An IAsyncEnumerable&lt;EventId&gt; that yields received event IDs</returns>
    /// <example>
    /// <code>
    /// await foreach (var eventId in listener.AsAsyncEnumerable(
    ///     deadline: TimeSpan.FromSeconds(1), 
    ///     token))
    /// {
    ///     Console.WriteLine($"Event: {eventId.Value}");
    /// }
    /// </code>
    /// </example>
#pragma warning disable CS1998 // Async method lacks 'await' operators (false positive - uses yield return)
    public static async IAsyncEnumerable<EventId> AsAsyncEnumerable(
        this Listener listener,
        TimeSpan? deadline = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
#pragma warning restore CS1998
    {
        if (listener == null)
            throw new ArgumentNullException(nameof(listener));

        using var waitset = WaitSetBuilder.New()
            .Create()
            .Expect("Failed to create WaitSet");

        using var guard = deadline.HasValue
            ? waitset.AttachDeadline(listener, deadline.Value).Expect("Failed to attach listener with deadline")
            : waitset.AttachNotification(listener).Expect("Failed to attach listener");

        while (!cancellationToken.IsCancellationRequested)
        {
            var waitResult = waitset.WaitAndProcess((attachmentId) =>
            {
                if (attachmentId.HasEventFrom(guard))
                {
                    if (deadline.HasValue && attachmentId.HasMissedDeadline(guard))
                    {
                        // Deadline missed
                        return CallbackProgression.Continue;
                    }

                    // Event available
                    return CallbackProgression.Stop;
                }
                return CallbackProgression.Continue;
            });

            if (!waitResult.IsOk)
            {
                yield break;
            }

            // Consume all pending events
            while (true)
            {
                var eventResult = listener.TryWait();
                if (eventResult.IsOk)
                {
                    var eventIdOpt = eventResult.Unwrap();
                    if (eventIdOpt.HasValue)
                    {
                        yield return eventIdOpt.Value;
                    }
                    else
                    {
                        break; // No more events
                    }
                }
                else
                {
                    yield break;
                }
            }
        }
    }

    private static class Observable
    {
        public static IObservable<T> Create<T>(Func<IObserver<T>, IDisposable> subscribe)
        {
            return System.Reactive.Linq.Observable.Create(subscribe);
        }
    }
}