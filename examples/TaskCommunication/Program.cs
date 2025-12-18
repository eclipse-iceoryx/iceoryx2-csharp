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
using System.Runtime.InteropServices;

namespace TaskCommunication;

/// <summary>
/// Demonstrates task-to-task communication within a single executable using iceoryx2.
/// This example shows how multiple async tasks can communicate using zero-copy shared memory.
/// </summary>
class Program
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SensorData
    {
        public ulong Timestamp;
        public double Temperature;
        public double Pressure;
        public int SensorId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ProcessedData
    {
        public ulong Timestamp;
        public double AverageTemperature;
        public double AveragePressure;
        public int SampleCount;
    }

    static async Task Main(string[] args)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("Task-to-Task Communication Example");
        Console.WriteLine("===========================================");
        Console.WriteLine();

        // Create a cancellation token source for graceful shutdown
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            Console.WriteLine("\nShutdown requested...");
            e.Cancel = true;
            cts.Cancel();
        };

        // Create a node for this process
        var nodeResult = NodeBuilder.New()
            .Name("task_communication_node")
            .Create();

        if (!nodeResult.IsOk)
        {
            Console.WriteLine($"Failed to create node: {nodeResult}");
            return;
        }

        using var node = nodeResult.Unwrap();

        // Create services for communication between tasks
        var sensorServiceResult = node.ServiceBuilder()
            .PublishSubscribe<SensorData>()
            .Open("sensor_data");

        var processedServiceResult = node.ServiceBuilder()
            .PublishSubscribe<ProcessedData>()
            .Open("processed_data");

        if (!sensorServiceResult.IsOk || !processedServiceResult.IsOk)
        {
            Console.WriteLine("Failed to create services");
            return;
        }

        using var sensorService = sensorServiceResult.Unwrap();
        using var processedService = processedServiceResult.Unwrap();

        // Start all tasks concurrently
        var tasks = new List<Task>
        {
            SensorTask(sensorService, 1, cts.Token),
            SensorTask(sensorService, 2, cts.Token),
            ProcessorTask(sensorService, processedService, cts.Token),
            DisplayTask(processedService, cts.Token)
        };

        Console.WriteLine("All tasks started. Press Ctrl+C to stop.\n");

        // Wait for all tasks to complete
        await Task.WhenAll(tasks);

        Console.WriteLine("\nAll tasks completed.");
    }

    /// <summary>
    /// Simulates a sensor that publishes data periodically
    /// </summary>
    static async Task SensorTask(Service sensorService, int sensorId, CancellationToken ct)
    {
        var publisherResult = sensorService.PublisherBuilder().Create();
        if (!publisherResult.IsOk)
        {
            Console.WriteLine($"[Sensor {sensorId}] Failed to create publisher");
            return;
        }

        using var publisher = publisherResult.Unwrap();
        var random = new Random(sensorId * 1000);
        var startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        Console.WriteLine($"[Sensor {sensorId}] Started");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Loan a sample for zero-copy writing
                var loanResult = publisher.Loan<SensorData>();
                if (loanResult.IsOk)
                {
                    using var sample = loanResult.Unwrap();

                    // Use zero-copy access to write directly to shared memory
                    ref var data = ref sample.GetPayloadRef();
                    data.Timestamp = (ulong)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - startTime);
                    data.Temperature = 20.0 + random.NextDouble() * 10.0;
                    data.Pressure = 1000.0 + random.NextDouble() * 50.0;
                    data.SensorId = sensorId;

                    var sendResult = sample.Send();
                    if (sendResult.IsOk)
                    {
                        Console.WriteLine($"[Sensor {sensorId}] Published: T={data.Temperature:F2}°C, P={data.Pressure:F2}hPa");
                    }
                }

                // Publish at different rates for each sensor
                await Task.Delay(sensorId == 1 ? 500 : 700, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        Console.WriteLine($"[Sensor {sensorId}] Stopped");
    }

    /// <summary>
    /// Processes sensor data and publishes aggregated results
    /// </summary>
    static async Task ProcessorTask(Service sensorService, Service processedService, CancellationToken ct)
    {
        var subscriberResult = sensorService.SubscriberBuilder().Create();
        var publisherResult = processedService.PublisherBuilder().Create();

        if (!subscriberResult.IsOk || !publisherResult.IsOk)
        {
            Console.WriteLine("[Processor] Failed to create subscriber or publisher");
            return;
        }

        using var subscriber = subscriberResult.Unwrap();
        using var publisher = publisherResult.Unwrap();

        Console.WriteLine("[Processor] Started");

        var samples = new List<SensorData>();
        var lastProcessTime = DateTimeOffset.UtcNow;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Receive sensor data with timeout
                var receiveResult = await subscriber.ReceiveAsync<SensorData>(
                    TimeSpan.FromMilliseconds(100), ct);

                if (receiveResult.IsOk)
                {
                    var sample = receiveResult.Unwrap();
                    if (sample != null)
                    {
                        using (sample)
                        {
                            // Use zero-copy read access
                            ref readonly var data = ref sample.GetPayloadRefReadOnly();
                            samples.Add(data);
                        }
                    }
                }

                // Process accumulated samples every 2 seconds
                if ((DateTimeOffset.UtcNow - lastProcessTime).TotalSeconds >= 2.0 && samples.Count > 0)
                {
                    var avgTemp = samples.Average(s => s.Temperature);
                    var avgPressure = samples.Average(s => s.Pressure);

                    // Publish processed data using zero-copy
                    var loanResult = publisher.Loan<ProcessedData>();
                    if (loanResult.IsOk)
                    {
                        using var processedSample = loanResult.Unwrap();

                        ref var processed = ref processedSample.GetPayloadRef();
                        processed.Timestamp = samples.Max(s => s.Timestamp);
                        processed.AverageTemperature = avgTemp;
                        processed.AveragePressure = avgPressure;
                        processed.SampleCount = samples.Count;

                        var sendResult = processedSample.Send();
                        if (sendResult.IsOk)
                        {
                            Console.WriteLine($"[Processor] Published aggregated data from {samples.Count} samples");
                        }
                    }

                    samples.Clear();
                    lastProcessTime = DateTimeOffset.UtcNow;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        Console.WriteLine("[Processor] Stopped");
    }

    /// <summary>
    /// Displays processed data
    /// </summary>
    static async Task DisplayTask(Service processedService, CancellationToken ct)
    {
        var subscriberResult = processedService.SubscriberBuilder().Create();
        if (!subscriberResult.IsOk)
        {
            Console.WriteLine("[Display] Failed to create subscriber");
            return;
        }

        using var subscriber = subscriberResult.Unwrap();

        Console.WriteLine("[Display] Started");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Wait for processed data
                var receiveResult = await subscriber.ReceiveAsync<ProcessedData>(
                    TimeSpan.FromSeconds(1), ct);

                if (receiveResult.IsOk)
                {
                    var sample = receiveResult.Unwrap();
                    if (sample != null)
                    {
                        using (sample)
                        {
                            // Use zero-copy read access
                            ref readonly var data = ref sample.GetPayloadRefReadOnly();

                            Console.WriteLine();
                            Console.WriteLine("╔════════════════════════════════════════╗");
                            Console.WriteLine("║      AGGREGATED SENSOR DATA            ║");
                            Console.WriteLine("╠════════════════════════════════════════╣");
                            Console.WriteLine($"║ Timestamp:    {data.Timestamp,10} ms      ║");
                            Console.WriteLine($"║ Avg Temp:     {data.AverageTemperature,10:F2} °C      ║");
                            Console.WriteLine($"║ Avg Pressure: {data.AveragePressure,10:F2} hPa     ║");
                            Console.WriteLine($"║ Sample Count: {data.SampleCount,10}         ║");
                            Console.WriteLine("╚════════════════════════════════════════╝");
                            Console.WriteLine();
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        Console.WriteLine("[Display] Stopped");
    }
}