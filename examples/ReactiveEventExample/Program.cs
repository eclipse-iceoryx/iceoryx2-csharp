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

using Iceoryx2;
using Iceoryx2.Reactive;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Reactive Event Example - Demonstrates event-driven Observable usage with Listener and Notifier.
/// 
/// This example shows how to use Iceoryx2.Reactive extensions with event-based communication,
/// which is truly event-driven (using WaitSet with epoll/kqueue) unlike polling-based pub/sub.
/// 
/// Key differences from ReactiveExample (pub/sub):
/// - Event-based (Listener/Notifier) = truly event-driven with WaitSet
/// - Pub/Sub (Subscriber/Publisher) = polling-based architecture
/// </summary>
class Program
{
    // Define various event types for demonstration
    static class EventTypes
    {
        public static readonly EventId SystemStartup = new(1);
        public static readonly EventId SystemShutdown = new(2);
        public static readonly EventId WarningAlert = new(10);
        public static readonly EventId ErrorAlert = new(11);
        public static readonly EventId CriticalAlert = new(12);
        public static readonly EventId PerformanceMetric = new(20);
        public static readonly EventId HeartBeat = new(30);
        public static readonly EventId DataReady = new(40);
        public static readonly EventId UserAction = new(50);
    }

    static async Task<int> Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Iceoryx2 Reactive Event Example");
            Console.WriteLine();
            Console.WriteLine("This example demonstrates event-driven Observable usage with Listener and Notifier.");
            Console.WriteLine("Events are truly asynchronous using WaitSet (epoll/kqueue), not polling.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run --framework net9.0 -- notifier SERVICE_NAME");
            Console.WriteLine("  dotnet run --framework net9.0 -- listener SERVICE_NAME");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  # Notifier sends various event types");
            Console.WriteLine("  dotnet run --framework net9.0 -- notifier events");
            Console.WriteLine();
            Console.WriteLine("  # Listener receives and processes using Rx operators");
            Console.WriteLine("  dotnet run --framework net9.0 -- listener events");
            return -1;
        }

        var command = args[0].ToLower();
        var serviceName = args.Length > 1 ? args[1] : "reactive_events";

        return command switch
        {
            "notifier" => await RunNotifierAsync(serviceName),
            "listener" => await RunListenerAsync(serviceName),
            _ => ShowUsage()
        };
    }

    static int ShowUsage()
    {
        Console.WriteLine("Unknown command. Use 'notifier' or 'listener'");
        return -1;
    }

    static async Task<int> RunNotifierAsync(string serviceName)
    {
        Console.WriteLine($"Starting event notifier for service '{serviceName}'...");
        Console.WriteLine("Triggering various events to demonstrate Rx operators");
        Console.WriteLine("Press Ctrl+C to stop\n");

        var node = NodeBuilder.New()
            .Name("notifier_node")
            .Create()
            .Expect("Failed to create node");

        var service = node.ServiceBuilder()
            .Event()
            .Open(serviceName)
            .Expect($"Failed to open service '{serviceName}'");

        var notifier = service.CreateNotifier(defaultEventId: EventTypes.HeartBeat)
            .Expect("Failed to create notifier");

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        var random = new Random();
        ulong counter = 0;

        // Event sequence simulation
        var events = new[]
        {
            EventTypes.SystemStartup,
            EventTypes.HeartBeat,
            EventTypes.HeartBeat,
            EventTypes.DataReady,
            EventTypes.PerformanceMetric,
            EventTypes.HeartBeat,
            EventTypes.UserAction,
            EventTypes.WarningAlert,
            EventTypes.HeartBeat,
            EventTypes.DataReady,
            EventTypes.HeartBeat,
            EventTypes.ErrorAlert,
            EventTypes.HeartBeat,
            EventTypes.CriticalAlert,
            EventTypes.HeartBeat,
            EventTypes.SystemShutdown
        };

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var eventId = events[(int)(counter % (ulong)events.Length)];

                notifier.Notify(eventId).Expect("Failed to notify");

                var eventName = GetEventName(eventId);
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Triggered: {eventName} (ID: {eventId.Value})");

                counter++;

                // Variable delay based on event type
                var delay = eventId.Value switch
                {
                    1 or 2 => 5000,  // System events - rare
                    11 or 12 => 2000, // Alerts - occasional
                    30 => 500,        // Heartbeat - frequent
                    _ => 1000         // Default
                };

                await Task.Delay(delay, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on Ctrl+C
        }

        Console.WriteLine("\nShutting down notifier...");
        notifier.Dispose();
        service.Dispose();
        node.Dispose();

        return 0;
    }

    static async Task<int> RunListenerAsync(string serviceName)
    {
        Console.WriteLine($"Starting Rx event listener for service '{serviceName}'...");
        Console.WriteLine("Demonstrating event-driven Observable with various Rx operators:\n");

        var node = NodeBuilder.New()
            .Name("listener_node")
            .Create()
            .Expect("Failed to create node");

        var service = node.ServiceBuilder()
            .Event()
            .Open(serviceName)
            .Expect($"Failed to open service '{serviceName}'");

        var listener = service.CreateListener()
            .Expect("Failed to create listener");

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        // ========================================
        // Example 1: Basic Event Observable
        // ========================================
        Console.WriteLine("═══ Example 1: Basic Event Observable (Event-Driven!) ═══");
        Console.WriteLine("Note: This uses WaitSet internally - true async events, no polling!\n");

        using var subscription1 = listener.AsObservable(cancellationToken: cts.Token)
            .Subscribe(
                eventId => Console.WriteLine($"[Basic] Event received: {GetEventName(eventId)} (ID: {eventId.Value})"),
                error => Console.WriteLine($"[Basic] Error: {error}"),
                () => Console.WriteLine("[Basic] Completed"));

        await Task.Delay(5000, cts.Token);

        // ========================================
        // Example 2: Filter Specific Events
        // ========================================
        Console.WriteLine("\n═══ Example 2: Filter Alert Events Only ═══");
        subscription1.Dispose();

        using var subscription2 = listener.AsObservable(cancellationToken: cts.Token)
            .Where(eventId => eventId.Value is >= 10 and < 20) // Alert range
            .Subscribe(eventId =>
                Console.WriteLine($"[ALERT!] {GetEventName(eventId)} (ID: {eventId.Value})"));

        await Task.Delay(5000, cts.Token);

        // ========================================
        // Example 3: Transform Events
        // ========================================
        Console.WriteLine("\n═══ Example 3: Transform to Event Summary ═══");
        subscription2.Dispose();

        using var subscription3 = listener.AsObservable(cancellationToken: cts.Token)
            .Select(eventId => new
            {
                Event = GetEventName(eventId),
                Id = eventId.Value,
                Category = GetEventCategory(eventId),
                Severity = GetEventSeverity(eventId),
                Timestamp = DateTime.Now
            })
            .Subscribe(summary =>
                Console.WriteLine($"[Summary] {summary.Timestamp:HH:mm:ss.fff} - {summary.Category}/{summary.Severity}: {summary.Event}"));

        await Task.Delay(5000, cts.Token);

        // ========================================
        // Example 4: Count Events by Type
        // ========================================
        Console.WriteLine("\n═══ Example 4: Count Events in 3-Second Windows ═══");
        subscription3.Dispose();

        using var subscription4 = listener.AsObservable(cancellationToken: cts.Token)
            .Buffer(TimeSpan.FromSeconds(3))
            .Where(batch => batch.Count > 0)
            .Subscribe(batch =>
            {
                var grouped = batch.GroupBy(e => GetEventName(e));
                Console.WriteLine($"[Window] Received {batch.Count} events:");
                foreach (var group in grouped)
                {
                    Console.WriteLine($"  - {group.Key}: {group.Count()} times");
                }
            });

        await Task.Delay(10000, cts.Token);

        // ========================================
        // Example 5: Critical Events Only
        // ========================================
        Console.WriteLine("\n═══ Example 5: Critical Events Only (High Priority) ═══");
        subscription4.Dispose();

        using var subscription5 = listener.AsObservable(cancellationToken: cts.Token)
            .Where(eventId => GetEventSeverity(eventId) == "Critical")
            .Subscribe(eventId =>
                Console.WriteLine($"[🚨 CRITICAL!] {GetEventName(eventId)} - Immediate action required!"));

        await Task.Delay(5000, cts.Token);

        // ========================================
        // Example 6: Throttle Heartbeats
        // ========================================
        Console.WriteLine("\n═══ Example 6: Throttle Heartbeats (Only Report After 1s Silence) ═══");
        subscription5.Dispose();

        using var subscription6 = listener.AsObservable(cancellationToken: cts.Token)
            .Where(eventId => eventId.Value == 30) // Heartbeat events
            .Throttle(TimeSpan.FromSeconds(1))
            .Subscribe(eventId =>
                Console.WriteLine($"[Throttled] Heartbeat stream paused for 1 second"));

        await Task.Delay(8000, cts.Token);

        // ========================================
        // Example 7: Event Sequences
        // ========================================
        Console.WriteLine("\n═══ Example 7: Detect Event Patterns (Startup -> Data Ready) ═══");
        subscription6.Dispose();

        var startupSeen = false;
        using var subscription7 = listener.AsObservable(cancellationToken: cts.Token)
            .Subscribe(eventId =>
            {
                if (eventId.Value == 1) // SystemStartup
                {
                    startupSeen = true;
                    Console.WriteLine("[Pattern] System startup detected - watching for data ready...");
                }
                else if (startupSeen && eventId.Value == 40) // DataReady
                {
                    Console.WriteLine("[Pattern] ✓ Complete startup sequence: System started and data ready!");
                    startupSeen = false;
                }
            });

        await Task.Delay(8000, cts.Token);

        // ========================================
        // Example 8: Async Enumerable
        // ========================================
        Console.WriteLine("\n═══ Example 8: Async Enumerable (await foreach) ═══");
        subscription7.Dispose();

        // var count = 0;
        // await foreach (var eventId in listener.AsAsyncEnumerable(cancellationToken: cts.Token))
        // {
        //     Console.WriteLine($"[AsyncEnum] {GetEventName(eventId)} (ID: {eventId.Value})");
        //     if (++count >= 5)
        //         break;
        // }

        // ========================================
        // Example 9: Deadline Monitoring
        // ========================================
        Console.WriteLine("\n═══ Example 9: Deadline Monitoring (Expect event within 2 seconds) ═══");
        Console.WriteLine("This will detect if no events arrive within the deadline\n");

        using var subscription9 = listener.AsObservable(
                deadline: TimeSpan.FromSeconds(2),
                cancellationToken: cts.Token)
            .Subscribe(eventId =>
            {
                Console.WriteLine($"[Deadline] Event received in time: {GetEventName(eventId)}");
            });

        await Task.Delay(10000, cts.Token);
        subscription9.Dispose();

        Console.WriteLine("\n✓ All examples completed!");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();

        listener.Dispose();
        service.Dispose();
        node.Dispose();

        return 0;
    }

    static string GetEventName(EventId eventId) => eventId.Value switch
    {
        1 => "SystemStartup",
        2 => "SystemShutdown",
        10 => "WarningAlert",
        11 => "ErrorAlert",
        12 => "CriticalAlert",
        20 => "PerformanceMetric",
        30 => "HeartBeat",
        40 => "DataReady",
        50 => "UserAction",
        _ => $"Unknown({eventId.Value})"
    };

    static string GetEventCategory(EventId eventId) => eventId.Value switch
    {
        1 or 2 => "System",
        >= 10 and < 20 => "Alert",
        20 => "Metric",
        30 => "Health",
        40 => "Data",
        50 => "User",
        _ => "Unknown"
    };

    static string GetEventSeverity(EventId eventId) => eventId.Value switch
    {
        1 or 2 => "Info",
        10 => "Warning",
        11 => "Error",
        12 => "Critical",
        20 or 30 or 40 or 50 => "Info",
        _ => "Unknown"
    };
}