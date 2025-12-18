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
using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Demonstrates the modern IAsyncEnumerable&lt;T&gt; API for WaitSet event processing.
/// This approach eliminates the busy-loop pitfall and provides a clean, idiomatic async/await pattern.
/// </summary>
class Program
{
    static async Task Main()
    {
        Console.WriteLine("=== WaitSet IAsyncEnumerable Demo ===\n");

        // Create node
        var node = NodeBuilder.New().Create().Expect("Failed to create node");

        // Create two event services
        var service1 = node.ServiceBuilder()
            .Event()
            .Create("service_1")
            .Expect("Failed to create service 1");

        var service2 = node.ServiceBuilder()
            .Event()
            .Create("service_2")
            .Expect("Failed to create service 2");

        // Create listeners
        var listener1 = service1.CreateListener().Expect("Failed to create listener 1");
        var listener2 = service2.CreateListener().Expect("Failed to create listener 2");

        // Create notifiers
        var notifier1 = service1.CreateNotifier().Expect("Failed to create notifier 1");
        var notifier2 = service2.CreateNotifier().Expect("Failed to create notifier 2");

        // Create WaitSet
        using var waitSet = WaitSetBuilder.New()
            .Create()
            .Expect("Failed to create WaitSet");

        // Attach listeners and keep the guards for comparison
        var guard1 = waitSet.AttachNotification(listener1).Unwrap();
        var guard2 = waitSet.AttachNotification(listener2).Unwrap();

        Console.WriteLine("✓ WaitSet created with 2 listener attachments");
        Console.WriteLine($"  Capacity: {waitSet.Capacity}, Length: {waitSet.Length}\n");

        // Create cancellation token for clean shutdown
        using var cts = new CancellationTokenSource();

        // Start event SENDER tasks (running independently on background threads)
        // This simulates events coming from other processes/threads
        var senderTask1 = Task.Run(async () =>
        {
            for (int i = 0; i < 5; i++)
            {
                await Task.Delay(200);
                var eventId = new EventId((ulong)i);
                notifier1.Notify(eventId).Expect($"Failed to notify event {i} on service 1");
                Console.WriteLine($"  → Sent event {i} to service 1");
            }
        }, cts.Token);

        var senderTask2 = Task.Run(async () =>
        {
            await Task.Delay(100); // Offset slightly from sender1
            for (int i = 0; i < 5; i++)
            {
                await Task.Delay(200);
                var eventId = new EventId((ulong)(i + 100));
                notifier2.Notify(eventId).Expect($"Failed to notify event {i + 100} on service 2");
                Console.WriteLine($"  → Sent event {i + 100} to service 2");
            }
        }, cts.Token);

        Console.WriteLine("Started background event senders...\n");

        // Start event RECEIVER in a background task
        var receiverTask = Task.Run(async () =>
        {
            Console.WriteLine("Started async event processing loop...\n");

            int receivedCount = 0;
            try
            {
                // This is the new, clean API - no callbacks, no busy-loop risk!
                await foreach (var evt in waitSet.Events(cts.Token))
                {
                    using (evt) // Dispose the event to clean up the attachment ID
                    {
                        // Simple pattern matching - no complex callback context needed
                        if (evt.IsFrom(guard1))
                        {
                            // CRITICAL: Drain ALL events from the listener to avoid busy-loop
                            // The WaitSet will keep waking up if we don't consume all pending events
                            while (true)
                            {
                                var eventId = listener1.TryWait().Unwrap();
                                if (eventId.HasValue)
                                {
                                    Console.WriteLine($"[Service 1] Received event: {eventId.Value}");
                                    receivedCount++;
                                }
                                else
                                {
                                    break; // No more events
                                }
                            }
                        }
                        else if (evt.IsFrom(guard2))
                        {
                            // CRITICAL: Drain ALL events from the listener to avoid busy-loop
                            while (true)
                            {
                                var eventId = listener2.TryWait().Unwrap();
                                if (eventId.HasValue)
                                {
                                    Console.WriteLine($"[Service 2] Received event: {eventId.Value}");
                                    receivedCount++;
                                }
                                else
                                {
                                    break; // No more events
                                }
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"\n✓ Event processing stopped gracefully (received {receivedCount} events)");
            }
        }, cts.Token);

        // Wait for senders to complete
        await Task.WhenAll(senderTask1, senderTask2);
        Console.WriteLine("\n✓ All events sent");

        // Give receiver time to process remaining events
        await Task.Delay(1000);

        // Shutdown
        Console.WriteLine("\nShutting down...");
        cts.Cancel();

        // Wait briefly for receiver to stop
        await Task.Delay(500);

        Console.WriteLine("\n=== Demo Complete ===");
    }
}