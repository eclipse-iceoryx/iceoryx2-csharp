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

namespace AsyncPubSubExample;

/// <summary>
/// Standalone async publish-subscribe example with async Main entry point.
/// Run with: dotnet run [publisher|subscriber|blocking|multi]
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("iceoryx2 C# Async Publish-Subscribe Example");
        Console.WriteLine("============================================\n");

        // Setup cancellation
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\nCancellation requested...");
            cts.Cancel();
        };

        if (args.Length == 0)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run publisher   - Run async publisher");
            Console.WriteLine("  dotnet run subscriber  - Run async subscriber with 5s timeout");
            Console.WriteLine("  dotnet run blocking    - Run async subscriber polling until data (no timeout)");
            Console.WriteLine("  dotnet run multi       - Run multiple concurrent subscribers");
            Console.WriteLine();
            Console.WriteLine("Note: 'blocking' mode uses efficient async polling (10ms intervals)");
            return;
        }

        var mode = args[0].ToLower();
        try
        {
            switch (mode)
            {
                case "publisher":
                    await RunPublisherAsync(cts.Token);
                    break;
                case "subscriber":
                    await RunSubscriberAsync(cts.Token);
                    break;
                case "blocking":
                    await RunSubscriberBlockingAsync(cts.Token);
                    break;
                case "multi":
                    await RunMultipleSubscribersAsync(cts.Token);
                    break;
                default:
                    Console.WriteLine($"Unknown mode: {mode}");
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\nOperation cancelled by user");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError: {ex.Message}");
        }
    }

    static async Task RunPublisherAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Starting Async Publisher...\n");

        using var node = NodeBuilder.New()
            .Name("csharp_async_publisher")
            .Create()
            .Expect("Failed to create node");

        Console.WriteLine($"Node created: {node.Name}");

        using var service = node.ServiceBuilder()
            .PublishSubscribe<int>()
            .Open("MyAsyncService")
            .Expect("Failed to open service");

        Console.WriteLine("Service opened");

        using var publisher = service.CreatePublisher()
            .Expect("Failed to create publisher");

        Console.WriteLine("Publisher created");
        Console.WriteLine("Press Ctrl+C to stop\n");

        var counter = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            var sample = publisher.Loan<int>()
                .Expect("Failed to loan sample");

            sample.Payload = counter;
            sample.Send().Expect("Failed to send sample");

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Sent: {counter}");

            counter++;
            await Task.Delay(1000, cancellationToken);
        }

        Console.WriteLine("\nPublisher stopped");
    }

    static async Task RunSubscriberAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Starting Async Subscriber (with 5s timeout)...\n");

        using var node = NodeBuilder.New()
            .Name("csharp_async_subscriber")
            .Create()
            .Expect("Failed to create node");

        Console.WriteLine($"Node created: {node.Name}");

        using var service = node.ServiceBuilder()
            .PublishSubscribe<int>()
            .Open("MyAsyncService")
            .Expect("Failed to open service");

        Console.WriteLine("Service opened");

        using var subscriber = service.SubscriberBuilder()
            .Create()
            .Expect("Failed to create subscriber");

        Console.WriteLine("Subscriber created");
        Console.WriteLine("Waiting for samples (async with timeout)...");
        Console.WriteLine("Press Ctrl+C to stop\n");

        while (!cancellationToken.IsCancellationRequested)
        {
            var result = await subscriber.ReceiveAsync<int>(
                TimeSpan.FromSeconds(5),
                cancellationToken);

            if (!result.IsOk)
            {
                Console.WriteLine($"Error receiving: {result}");
                break;
            }

            var sample = result.Unwrap();
            if (sample != null)
            {
                using (sample)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Received: {sample.Payload}");
                }
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Timeout - no sample received");
            }
        }

        Console.WriteLine("\nSubscriber stopped");
    }

    static async Task RunSubscriberBlockingAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Starting Async Subscriber (polling until data)...\n");

        using var node = NodeBuilder.New()
            .Name("csharp_async_blocking_subscriber")
            .Create()
            .Expect("Failed to create node");

        Console.WriteLine($"Node created: {node.Name}");

        using var service = node.ServiceBuilder()
            .PublishSubscribe<int>()
            .Open("MyAsyncService")
            .Expect("Failed to open service");

        Console.WriteLine("Service opened");

        using var subscriber = service.SubscriberBuilder()
            .Create()
            .Expect("Failed to create subscriber");

        Console.WriteLine("Subscriber created");
        Console.WriteLine("Waiting for samples (polling async, no timeout)...");
        Console.WriteLine("Note: Polls every 10ms but yields to thread pool efficiently");
        Console.WriteLine("Press Ctrl+C to stop\n");

        while (!cancellationToken.IsCancellationRequested)
        {
            var result = await subscriber.ReceiveAsync<int>(cancellationToken);

            if (!result.IsOk)
            {
                Console.WriteLine($"Error receiving: {result}");
                break;
            }

            var sample = result.Unwrap();
            using (sample)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Received: {sample.Payload}");
            }
        }

        Console.WriteLine("\nSubscriber stopped");
    }

    static async Task RunMultipleSubscribersAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Starting 3 Concurrent Async Subscribers...\n");

        using var node = NodeBuilder.New()
            .Name("csharp_multi_subscriber")
            .Create()
            .Expect("Failed to create node");

        using var service = node.ServiceBuilder()
            .PublishSubscribe<int>()
            .Open("MyAsyncService")
            .Expect("Failed to open service");

        using var subscriber1 = service.SubscriberBuilder().Create().Expect("Failed to create subscriber 1");
        using var subscriber2 = service.SubscriberBuilder().Create().Expect("Failed to create subscriber 2");
        using var subscriber3 = service.SubscriberBuilder().Create().Expect("Failed to create subscriber 3");

        Console.WriteLine("Created 3 subscribers");
        Console.WriteLine("Each subscriber will process data concurrently");
        Console.WriteLine("Press Ctrl+C to stop\n");

        var tasks = new[]
        {
            ProcessSubscriberAsync("Sub-1", subscriber1, cancellationToken),
            ProcessSubscriberAsync("Sub-2", subscriber2, cancellationToken),
            ProcessSubscriberAsync("Sub-3", subscriber3, cancellationToken)
        };

        await Task.WhenAll(tasks);

        Console.WriteLine("\nAll subscribers stopped");
    }

    static async Task ProcessSubscriberAsync(
        string name,
        Subscriber subscriber,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
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
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{name}] Received: {sample.Payload}");
                    }
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{name}] Timeout");
                }
            }
        }

        Console.WriteLine($"[{name}] Stopped");
    }
}