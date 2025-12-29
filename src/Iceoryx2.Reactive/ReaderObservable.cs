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
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace Iceoryx2.Reactive;

/// <summary>
/// Internal implementation of IObservable&lt;TValue&gt; for iceoryx2 Blackboard Reader.
/// Continuously polls the reader and pushes the current value to observers.
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <typeparam name="TValue">The type of the value.</typeparam>
internal sealed class ReaderObservable<TKey, TValue> : IObservable<TValue>
    where TKey : unmanaged
    where TValue : unmanaged
{
    private readonly Reader<TKey> _reader;
    private readonly TKey _key;
    private readonly TimeSpan _pollingInterval;
    private readonly CancellationToken _cancellationToken;

    public ReaderObservable(Reader<TKey> reader, TKey key, TimeSpan pollingInterval, CancellationToken cancellationToken)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _key = key;
        _pollingInterval = pollingInterval;
        _cancellationToken = cancellationToken;
    }

    public IDisposable Subscribe(IObserver<TValue> observer)
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
                        // Try to get the entry and read the value
                        var entryResult = _reader.Entry<TValue>(_key);

                        if (entryResult.IsOk)
                        {
                            using var entry = entryResult.Unwrap();
                            var valueResult = entry.Get();

                            if (valueResult.IsOk)
                            {
                                // Push the value to the observer
                                observer.OnNext(valueResult.Unwrap());
                            }
                            else
                            {
                                observer.OnError(new InvalidOperationException("Failed to read blackboard value"));
                                break;
                            }
                        }
                        else
                        {
                            // On error, notify observer and complete
                            observer.OnError(new InvalidOperationException("Failed to access blackboard entry"));
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