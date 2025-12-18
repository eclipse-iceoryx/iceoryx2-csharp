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
using System.Reactive.Linq;
using System.Runtime.InteropServices;

// Define data structure matching the Rust/C type
[StructLayout(LayoutKind.Sequential)]
[Iox2Type("11SensorData")]
public struct SensorData
{
    public double Temperature;
    public double Humidity;
    public ulong Timestamp;

    public override string ToString() =>
        $"Temp: {Temperature:F1}°C, Humidity: {Humidity:F1}%, Time: {Timestamp}";
}

class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Iceoryx2 Reactive Extensions Example");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run --framework net9.0 -- publish SERVICE_NAME");
            Console.WriteLine("  dotnet run --framework net9.0 -- subscribe SERVICE_NAME");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  # Publisher sends sensor data every 500ms");
            Console.WriteLine("  dotnet run --framework net9.0 -- publish sensors");
            Console.WriteLine();
            Console.WriteLine("  # Subscriber receives and processes using Rx operators");
            Console.WriteLine("  dotnet run --framework net9.0 -- subscribe sensors");
            return -1;
        }

        var command = args[0].ToLower();
        var serviceName = args.Length > 1 ? args[1] : "reactive_demo";

        return command switch
        {
            "publish" => await RunPublisherAsync(serviceName),
            "subscribe" => await RunSubscriberAsync(serviceName),
            _ => ShowUsage()
        };
    }

    static int ShowUsage()
    {
        Console.WriteLine("Unknown command. Use 'publish' or 'subscribe'");
        return -1;
    }

    static async Task<int> RunPublisherAsync(string serviceName)
    {
        Console.WriteLine($"Starting publisher for service '{serviceName}'...");
        Console.WriteLine("Publishing sensor data every 500ms");
        Console.WriteLine("Press Ctrl+C to stop\n");

        var node = NodeBuilder.New().Create().Expect("Failed to create node");

        var service = node.ServiceBuilder()
            .PublishSubscribe<SensorData>()
            .Open(serviceName)
            .Expect($"Failed to open service '{serviceName}'");

        var publisher = service.CreatePublisher()
            .Expect("Failed to create publisher");

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        var random = new Random();
        ulong counter = 0;

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var data = new SensorData
                {
                    Temperature = 20.0 + random.NextDouble() * 15.0,
                    Humidity = 40.0 + random.NextDouble() * 30.0,
                    Timestamp = counter++
                };

                publisher.SendCopy(data).Expect("Failed to send sample");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Published: {data}");

                await Task.Delay(500, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on Ctrl+C
        }

        Console.WriteLine("\nShutting down publisher...");
        publisher.Dispose();
        service.Dispose();
        node.Dispose();

        return 0;
    }

    static async Task<int> RunSubscriberAsync(string serviceName)
    {
        Console.WriteLine($"Starting Rx subscriber for service '{serviceName}'...");
        Console.WriteLine("Demonstrating various Rx operators:\n");

        var node = NodeBuilder.New().Create().Expect("Failed to create node");

        var service = node.ServiceBuilder()
            .PublishSubscribe<SensorData>()
            .Open(serviceName)
            .Expect($"Failed to open service '{serviceName}'");

        var subscriber = service.SubscriberBuilder()
            .Create()
            .Expect("Failed to create subscriber");

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        // ========================================
        // Example 1: Basic Observable
        // ========================================
        Console.WriteLine("═══ Example 1: Basic Observable ═══");
        using var subscription1 = subscriber.AsObservable<SensorData>(cancellationToken: cts.Token)
            .Subscribe(
                data => Console.WriteLine($"[Basic] {data}"),
                error => Console.WriteLine($"[Basic] Error: {error}"),
                () => Console.WriteLine("[Basic] Completed"));

        await Task.Delay(2000, cts.Token);

        // ========================================
        // Example 2: Filtering with Where
        // ========================================
        Console.WriteLine("\n═══ Example 2: Filter High Temperature (>28°C) ═══");
        subscription1.Dispose();

        using var subscription2 = subscriber.AsObservable<SensorData>(cancellationToken: cts.Token)
            .Where(data => data.Temperature > 28.0)
            .Subscribe(data =>
                Console.WriteLine($"[HOT!] {data.Temperature:F1}°C at timestamp {data.Timestamp}"));

        await Task.Delay(3000, cts.Token);

        // ========================================
        // Example 3: Transformation with Select
        // ========================================
        Console.WriteLine("\n═══ Example 3: Transform to Summary ═══");
        subscription2.Dispose();

        using var subscription3 = subscriber.AsObservable<SensorData>(cancellationToken: cts.Token)
            .Select(data => new
            {
                Temp = data.Temperature,
                IsCritical = data.Temperature > 30.0,
                IsComfortable = data.Humidity > 40.0 && data.Humidity < 60.0
            })
            .Subscribe(summary =>
                Console.WriteLine($"[Summary] {summary.Temp:F1}°C, Critical: {summary.IsCritical}, Comfortable: {summary.IsComfortable}"));

        await Task.Delay(3000, cts.Token);

        // ========================================
        // Example 4: Buffering
        // ========================================
        Console.WriteLine("\n═══ Example 4: Process in Batches (2 second windows) ═══");
        subscription3.Dispose();

        using var subscription4 = subscriber.AsObservable<SensorData>(cancellationToken: cts.Token)
            .Buffer(TimeSpan.FromSeconds(2))
            .Where(batch => batch.Count > 0)
            .Subscribe(batch =>
            {
                var avgTemp = batch.Average(d => d.Temperature);
                var avgHumidity = batch.Average(d => d.Humidity);
                Console.WriteLine($"[Batch] {batch.Count} samples - Avg Temp: {avgTemp:F1}°C, Avg Humidity: {avgHumidity:F1}%");
            });

        await Task.Delay(6000, cts.Token);

        // ========================================
        // Example 5: Throttling with Sample
        // ========================================
        Console.WriteLine("\n═══ Example 5: Sample Every 1 Second (Throttle) ═══");
        subscription4.Dispose();

        using var subscription5 = subscriber.AsObservable<SensorData>(cancellationToken: cts.Token)
            .Sample(TimeSpan.FromSeconds(1))
            .Subscribe(data =>
                Console.WriteLine($"[Sample] {data}"));

        await Task.Delay(5000, cts.Token);

        // ========================================
        // Example 6: Distinct Until Changed
        // ========================================
        Console.WriteLine("\n═══ Example 6: Only When Temperature Changes Significantly (±1°C) ═══");
        subscription5.Dispose();

        using var subscription6 = subscriber.AsObservable<SensorData>(cancellationToken: cts.Token)
            .Select(data => (int)data.Temperature) // Convert to integer temperature
            .DistinctUntilChanged() // Only emit when temperature changes
            .Subscribe(temp =>
                Console.WriteLine($"[Changed] Temperature changed to {temp}°C"));

        await Task.Delay(5000, cts.Token);

        // ========================================
        // Example 7: Async Enumerable (await foreach)
        // ========================================
        Console.WriteLine("\n═══ Example 7: Async Enumerable (await foreach) ═══");
        subscription6.Dispose();

        var count = 0;
        await foreach (var data in subscriber.AsAsyncEnumerable<SensorData>(cancellationToken: cts.Token))
        {
            Console.WriteLine($"[AsyncEnum] {data}");
            if (++count >= 5)
                break;
        }

        Console.WriteLine("\n✓ All examples completed!");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();

        subscriber.Dispose();
        service.Dispose();
        node.Dispose();

        return 0;
    }
}