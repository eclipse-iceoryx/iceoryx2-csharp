// Copyright (c) 2024 Contributors to the Eclipse Foundation
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

using Iceoryx2;
using System.Reactive.Disposables;
using System.Reactive.Linq;

/// <summary>
/// Represents an event received from a service with metadata
/// </summary>
record EventNotification(string ServiceName, EventId EventId, DateTime Timestamp);

/// <summary>
/// Extension methods for creating Observables from WaitSet
/// </summary>
static class WaitSetObservableExtensions
{
    /// <summary>
    /// Creates an Observable stream from a WaitSet that emits events
    /// </summary>
    public static IObservable<EventNotification> ToObservable(
        this WaitSet waitSet,
        WaitSetGuard[] guards,
        Listener[] listeners,
        string[] serviceNames,
        CancellationToken cancellationToken = default)
    {
        return Observable.Create<EventNotification>(observer =>
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Event processing callback
            CallbackProgression OnEvent(WaitSetAttachmentId attachmentId)
            {
                if (cts.Token.IsCancellationRequested)
                    return CallbackProgression.Stop;

                for (int i = 0; i < guards.Length; i++)
                {
                    if (attachmentId.HasEventFrom(guards[i]))
                    {
                        // Consume ALL pending events to avoid busy loop
                        while (true)
                        {
                            var eventResult = listeners[i].TryWait();
                            if (eventResult.IsOk)
                            {
                                var eventIdOpt = eventResult.Unwrap();
                                if (eventIdOpt.HasValue)
                                {
                                    var notification = new EventNotification(
                                        serviceNames[i],
                                        eventIdOpt.Value,
                                        DateTime.Now
                                    );

                                    try
                                    {
                                        observer.OnNext(notification);
                                    }
                                    catch (Exception ex)
                                    {
                                        observer.OnError(ex);
                                        return CallbackProgression.Stop;
                                    }
                                }
                                else
                                {
                                    break; // No more events
                                }
                            }
                            else
                            {
                                break; // Error occurred
                            }
                        }

                        break;
                    }
                }

                return CallbackProgression.Continue;
            }

            // Run WaitSet in background task
            var waitTask = Task.Run(() =>
            {
                try
                {
                    var result = waitSet.WaitAndProcess(OnEvent);
                    observer.OnCompleted();
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }
            }, cts.Token);

            // Return disposable that stops the WaitSet
            return new CompositeDisposable(
                cts,
                Disposable.Create(() =>
                {
                    waitSet.Stop();
                    try
                    {
                        waitTask.Wait(TimeSpan.FromSeconds(5));
                    }
                    catch
                    {
                        // Ignore timeout/cancellation
                    }
                })
            );
        });
    }
}

class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run observe SERVICE_NAME_1 [SERVICE_NAME_2 ...]");
            Console.WriteLine("  dotnet run notify EVENT_ID SERVICE_NAME");
            return -1;
        }

        var command = args[0];

        if (command == "observe")
        {
            return await RunObserverAsync(args.Skip(1).ToArray());
        }
        else if (command == "notify")
        {
            return await RunNotifierAsync(args.Skip(1).ToArray());
        }
        else
        {
            Console.WriteLine($"Unknown command: {command}");
            Console.WriteLine("Valid commands: observe, notify");
            return -1;
        }
    }

    static async Task<int> RunObserverAsync(string[] serviceNames)
    {
        if (serviceNames.Length == 0)
        {
            Console.WriteLine("Usage: dotnet run observe SERVICE_NAME_1 [SERVICE_NAME_2 ...]");
            return -1;
        }

        Console.WriteLine($"Creating Observable for services: {string.Join(", ", serviceNames.Select(s => $"'{s}'"))}");

        // Create node
        var node = NodeBuilder.New().Create().Expect("Failed to create node");

        // Create event services
        var services = serviceNames
            .Select(name => node.ServiceBuilder()
                .Event()
                .Open(name)
                .Expect($"Failed to open service '{name}'"))
            .ToArray();

        // Create listeners
        var listeners = services
            .Select(service => service.CreateListener()
                .Expect("Failed to create listener"))
            .ToArray();

        // Create WaitSet
        var waitSet = WaitSetBuilder.New()
            .SignalHandling(SignalHandlingMode.TerminationAndInterrupt)
            .Create()
            .Expect("Failed to create WaitSet");

        // Attach all listeners
        var guards = listeners
            .Select(listener => waitSet.AttachNotification(listener)
                .Expect("Failed to attach listener"))
            .ToArray();

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        // Create Observable stream from WaitSet
        var eventStream = waitSet.ToObservable(guards, listeners, serviceNames, cts.Token);

        // Example 1: Simple subscription - print all events
        Console.WriteLine("\n=== Simple Event Stream ===");
        var subscription1 = eventStream
            .Subscribe(
                onNext: evt => Console.WriteLine($"[{evt.Timestamp:HH:mm:ss.fff}] Service '{evt.ServiceName}' → Event ID: {evt.EventId.Value}"),
                onError: ex => Console.WriteLine($"Error: {ex.Message}"),
                onCompleted: () => Console.WriteLine("Event stream completed")
            );

        // Example 2: Filter events by service name
        Console.WriteLine("\n=== Filtered Stream (specific service) ===");
        if (serviceNames.Length > 0)
        {
            var subscription2 = eventStream
                .Where(evt => evt.ServiceName == serviceNames[0])
                .Subscribe(evt => Console.WriteLine($"  Filtered: {evt.ServiceName} → {evt.EventId.Value}"));
        }

        // Example 3: Group events by service and count
        Console.WriteLine("\n=== Event Counting (every 5 seconds) ===");
        var subscription3 = eventStream
            .GroupBy(evt => evt.ServiceName)
            .SelectMany(group =>
                group.Buffer(TimeSpan.FromSeconds(5))
                     .Where(buffer => buffer.Count > 0)
                     .Select(buffer => new { Service = group.Key, Count = buffer.Count })
            )
            .Subscribe(stats => Console.WriteLine($"  Stats: '{stats.Service}' received {stats.Count} events in last 5s"));

        // Example 4: Throttle events - only process one per second per service
        Console.WriteLine("\n=== Throttled Stream (1/sec max per service) ===");
        var subscription4 = eventStream
            .GroupBy(evt => evt.ServiceName)
            .SelectMany(group =>
                group.Throttle(TimeSpan.FromSeconds(1))
                     .Select(evt => $"Throttled: {evt.ServiceName} → {evt.EventId.Value}")
            )
            .Subscribe(msg => Console.WriteLine($"  {msg}"));

        // Example 5: Combine events from multiple services
        Console.WriteLine("\n=== Combined Stream (zip multiple services) ===");
        if (serviceNames.Length >= 2)
        {
            var service1Stream = eventStream.Where(e => e.ServiceName == serviceNames[0]);
            var service2Stream = eventStream.Where(e => e.ServiceName == serviceNames[1]);

            var subscription5 = service1Stream
                .Zip(service2Stream, (e1, e2) =>
                    $"Pair: [{e1.ServiceName}:{e1.EventId.Value}] + [{e2.ServiceName}:{e2.EventId.Value}]")
                .Subscribe(msg => Console.WriteLine($"  {msg}"));
        }

        // Example 6: Async processing with SelectMany
        Console.WriteLine("\n=== Async Processing ===");
        var subscription6 = eventStream
            .SelectMany(async evt =>
            {
                // Simulate async processing
                await Task.Delay(10);
                return $"Processed: {evt.ServiceName} → {evt.EventId.Value}";
            })
            .Subscribe(msg => Console.WriteLine($"  {msg}"));

        Console.WriteLine("\n✓ All Observable subscriptions active. Press Ctrl+C to stop...\n");

        // Wait for cancellation
        try
        {
            await Task.Delay(Timeout.Infinite, cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\n\nShutting down...");
        }

        // Cleanup subscriptions
        subscription1.Dispose();
        subscription3.Dispose();
        subscription4.Dispose();
        subscription6.Dispose();

        // Cleanup guards
        foreach (var guard in guards)
            guard.Dispose();

        // Cleanup
        waitSet.Dispose();
        foreach (var listener in listeners)
            listener.Dispose();
        foreach (var service in services)
            service.Dispose();
        node.Dispose();

        return 0;
    }

    static async Task<int> RunNotifierAsync(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: dotnet run notify EVENT_ID SERVICE_NAME");
            return -1;
        }

        if (!ulong.TryParse(args[0], out var eventIdValue))
        {
            Console.WriteLine($"Invalid EVENT_ID: {args[0]}");
            return -1;
        }

        var serviceName = args[1];

        // Create node
        var node = NodeBuilder.New().Create().Expect("Failed to create node");

        // Create event service
        var service = node.ServiceBuilder()
            .Event()
            .Open(serviceName)
            .Expect($"Failed to create service '{serviceName}'");

        // Create notifier
        var notifier = service.CreateNotifier()
            .Expect($"Failed to create notifier for '{serviceName}'");

        Console.WriteLine($"Sending events with ID {eventIdValue} to service '{serviceName}'");

        var eventId = new EventId(eventIdValue);
        var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                notifier.Notify(eventId)
                    .Expect("Failed to notify listener");

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Sent event {eventIdValue} to '{serviceName}'");

                await Task.Delay(1000, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        Console.WriteLine("\nShutting down...");

        // Cleanup
        notifier.Dispose();
        service.Dispose();
        node.Dispose();

        return 0;
    }
}