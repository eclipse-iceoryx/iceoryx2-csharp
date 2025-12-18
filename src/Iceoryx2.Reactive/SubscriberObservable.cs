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
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace Iceoryx2.Reactive;

/// <summary>
/// Internal implementation of IObservable&lt;T&gt; for iceoryx2 Subscriber.
/// Continuously polls the subscriber and pushes received samples to observers.
/// </summary>
/// <typeparam name="T">The unmanaged type of data being received</typeparam>
internal sealed class SubscriberObservable<T> : IObservable<T> where T : unmanaged
{
    private readonly Subscriber _subscriber;
    private readonly TimeSpan _pollingInterval;
    private readonly CancellationToken _cancellationToken;

    public SubscriberObservable(Subscriber subscriber, TimeSpan pollingInterval, CancellationToken cancellationToken)
    {
        _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
        _pollingInterval = pollingInterval;
        _cancellationToken = cancellationToken;
    }

    public IDisposable Subscribe(IObserver<T> observer)
    {
        if (observer == null)
            throw new ArgumentNullException(nameof(observer));

        var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken);

        // Start polling task
        var pollingTask = Task.Run(async () =>
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        // Try to receive a sample
                        var result = _subscriber.Receive<T>();

                        if (result.IsOk)
                        {
                            var sample = result.Unwrap();
                            if (sample != null)
                            {
                                // Push the payload to the observer
                                observer.OnNext(sample.Payload);
                                // Dispose the sample
                                sample.Dispose();
                            }
                        }
                        else
                        {
                            // On error, notify observer and complete
                            observer.OnError(new InvalidOperationException("Failed to receive sample"));
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        observer.OnError(ex);
                        break;
                    }

                    // Wait for next polling interval
                    await Task.Delay(_pollingInterval, cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on cancellation - complete gracefully
            }
            finally
            {
                observer.OnCompleted();
            }
        }, cts.Token);

        // Return a disposable that cancels the polling task
        return Disposable.Create(() =>
        {
            cts.Cancel();
            try
            {
                pollingTask.Wait(TimeSpan.FromSeconds(1));
            }
            catch (AggregateException)
            {
                // Expected if task was cancelled
            }
            cts.Dispose();
        });
    }
}