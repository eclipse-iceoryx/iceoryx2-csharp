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

namespace PublishSubscribeExample;

/// <summary>
/// Async/await version of the publish-subscribe example demonstrating modern C# async patterns.
/// This example shows how to use the async methods for waiting on data without blocking threads.
/// </summary>
class ProgramAsync
{
    /// <summary>
    /// Async publisher that sends incrementing counter values.
    /// This example is similar to the sync version but uses async/await for delays.
    /// </summary>
    public static async Task RunPublisherAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Starting Async Publisher...\n");

        // Create a node
        using var node = NodeBuilder.New()
            .Name("csharp_async_publisher")
            .Create()
            .Expect("Failed to create node");

        Console.WriteLine($"Node created: {node.Name}");

        // Open or create a service
        using var service = node.ServiceBuilder()
            .PublishSubscribe<int>()
            .Open("MyAsyncService")
            .Expect("Failed to open service");

        Console.WriteLine("Service opened");

        // Create a publisher
        using var publisher = service.CreatePublisher()
            .Expect("Failed to create publisher");

        Console.WriteLine("Publisher created\n");

        // Publish data
        var counter = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            var sample = publisher.Loan<int>()
                .Expect("Failed to loan sample");

            sample.Payload = counter;

            sample.Send()
                .Expect("Failed to send sample");

            Console.WriteLine($"Sent: {counter}");

            counter++;

            // Use async delay instead of Thread.Sleep
            await Task.Delay(1000, cancellationToken);
        }

        Console.WriteLine("\nPublisher shutting down...");
    }

    /// <summary>
    /// Async subscriber that waits for data using the ReceiveAsync method.
    /// Demonstrates proper async/await usage with timeout and cancellation support.
    /// </summary>
    public static async Task RunSubscriberAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Starting Async Subscriber...\n");

        // Create a node
        using var node = NodeBuilder.New()
            .Name("csharp_async_subscriber")
            .Create()
            .Expect("Failed to create node");

        Console.WriteLine($"Node created: {node.Name}");

        // Open the service
        using var service = node.ServiceBuilder()
            .PublishSubscribe<int>()
            .Open("MyAsyncService")
            .Expect("Failed to open service");

        Console.WriteLine("Service opened");

        // Create a subscriber
        using var subscriber = service.SubscriberBuilder()
            .Create()
            .Expect("Failed to create subscriber");

        Console.WriteLine("Subscriber created\n");
        Console.WriteLine("Waiting for samples (async)...\n");

        // Receive data asynchronously
        while (!cancellationToken.IsCancellationRequested)
        {
            // Wait up to 5 seconds for a sample (async, non-blocking)
            var receiveResult = await subscriber.ReceiveAsync<int>(
                TimeSpan.FromSeconds(5),
                cancellationToken);

            if (!receiveResult.IsOk)
            {
                Console.WriteLine($"Error receiving: {receiveResult}");
                break;
            }

            var sample = receiveResult.Unwrap();

            if (sample != null)
            {
                using (sample)
                {
                    Console.WriteLine($"Received: {sample.Payload}");
                }
            }
            else
            {
                Console.WriteLine("Timeout - no sample received within 5 seconds");
            }
        }

        Console.WriteLine("\nSubscriber shutting down...");
    }

    /// <summary>
    /// Async subscriber that waits indefinitely for data.
    /// Demonstrates the blocking async variant that waits until data arrives.
    /// </summary>
    public static async Task RunSubscriberBlockingAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Starting Async Blocking Subscriber...\n");

        // Create a node
        using var node = NodeBuilder.New()
            .Name("csharp_async_blocking_subscriber")
            .Create()
            .Expect("Failed to create node");

        Console.WriteLine($"Node created: {node.Name}");

        // Open the service
        using var service = node.ServiceBuilder()
            .PublishSubscribe<int>()
            .Open("MyAsyncService")
            .Expect("Failed to open service");

        Console.WriteLine("Service opened");

        // Create a subscriber
        using var subscriber = service.SubscriberBuilder()
            .Create()
            .Expect("Failed to create subscriber");

        Console.WriteLine("Subscriber created\n");
        Console.WriteLine("Waiting for samples (blocking async)...\n");

        // Receive data asynchronously - blocks until data arrives
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Wait indefinitely for a sample (async, with cancellation)
                var receiveResult = await subscriber.ReceiveAsync<int>(cancellationToken);

                if (!receiveResult.IsOk)
                {
                    Console.WriteLine($"Error receiving: {receiveResult}");
                    break;
                }

                var sample = receiveResult.Unwrap();

                using (sample)
                {
                    Console.WriteLine($"Received: {sample.Payload}");
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\nReceive cancelled");
                break;
            }
        }

        Console.WriteLine("\nSubscriber shutting down...");
    }

    /// <summary>
    /// Example showing multiple subscribers processing data concurrently.
    /// Demonstrates Task composition with async pub/sub.
    /// </summary>
    public static async Task RunMultipleSubscribersAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Starting Multiple Async Subscribers...\n");

        // Create a node
        using var node = NodeBuilder.New()
            .Name("csharp_multi_subscriber")
            .Create()
            .Expect("Failed to create node");

        // Open the service
        using var service = node.ServiceBuilder()
            .PublishSubscribe<int>()
            .Open("MyAsyncService")
            .Expect("Failed to open service");

        // Create multiple subscribers
        using var subscriber1 = service.SubscriberBuilder().Create().Expect("Failed to create subscriber 1");
        using var subscriber2 = service.SubscriberBuilder().Create().Expect("Failed to create subscriber 2");
        using var subscriber3 = service.SubscriberBuilder().Create().Expect("Failed to create subscriber 3");

        Console.WriteLine("Created 3 subscribers\n");

        // Process each subscriber concurrently
        var tasks = new[]
        {
            ProcessSubscriberAsync("Subscriber-1", subscriber1, cancellationToken),
            ProcessSubscriberAsync("Subscriber-2", subscriber2, cancellationToken),
            ProcessSubscriberAsync("Subscriber-3", subscriber3, cancellationToken)
        };

        await Task.WhenAll(tasks);

        Console.WriteLine("\nAll subscribers shut down");
    }

    private static async Task ProcessSubscriberAsync(
        string name,
        Subscriber subscriber,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await subscriber.ReceiveAsync<int>(
                    TimeSpan.FromSeconds(10),
                    cancellationToken);

                if (result.IsOk)
                {
                    var sample = result.Unwrap();
                    if (sample != null)
                    {
                        using (sample)
                        {
                            Console.WriteLine($"[{name}] Received: {sample.Payload}");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        Console.WriteLine($"[{name}] Shutting down");
    }

    // Example of how to use this in a Main method:
    //
    // static async Task Main(string[] args)
    // {
    //     var cts = new CancellationTokenSource();
    //     Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };
    //
    //     if (args.Length == 0)
    //     {
    //         Console.WriteLine("Usage:");
    //         Console.WriteLine("  dotnet run publisher   - Run async publisher");
    //         Console.WriteLine("  dotnet run subscriber  - Run async subscriber with timeout");
    //         Console.WriteLine("  dotnet run blocking    - Run async subscriber blocking until data");
    //         Console.WriteLine("  dotnet run multi       - Run multiple concurrent subscribers");
    //         return;
    //     }
    //
    //     var mode = args[0].ToLower();
    //     switch (mode)
    //     {
    //         case "publisher":
    //             await RunPublisherAsync(cts.Token);
    //             break;
    //         case "subscriber":
    //             await RunSubscriberAsync(cts.Token);
    //             break;
    //         case "blocking":
    //             await RunSubscriberBlockingAsync(cts.Token);
    //             break;
    //         case "multi":
    //             await RunMultipleSubscribersAsync(cts.Token);
    //             break;
    //         default:
    //             Console.WriteLine($"Unknown mode: {mode}");
    //             break;
    //     }
    // }
}