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
using Iceoryx2.Blackboard;
using System;
using System.Runtime.InteropServices;

namespace Blackboard;

/// <summary>
/// Key type for blackboard entries - represents different sensors
/// </summary>
public enum SensorKey : int
{
    Temperature = 0,
    Humidity = 1,
    Pressure = 2
}

/// <summary>
/// Value type for sensor data
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct SensorData
{
    public double Value;
    public long Timestamp;
    public int Quality;
}

class Program
{
    const string SERVICE_NAME = "Blackboard/Sensors";

    static void Main(string[] args)
    {
        if (args.Length == 0 || (args[0] != "creator" && args[0] != "opener"))
        {
            Console.WriteLine("Usage: Blackboard [creator|opener]");
            Console.WriteLine("");
            Console.WriteLine("  creator - Create the blackboard and write sensor data");
            Console.WriteLine("  opener  - Open the blackboard and read sensor data");
            return;
        }

        if (args[0] == "creator")
        {
            RunCreator();
        }
        else
        {
            RunOpener();
        }
    }

    static void RunCreator()
    {
        Console.WriteLine("Starting blackboard creator...");

        // Create node
        var nodeResult = NodeBuilder.New()
            .Name("blackboard_creator")
            .Create();

        if (!nodeResult.IsOk)
        {
            Console.WriteLine($"Failed to create node: {nodeResult}");
            return;
        }

        using var node = nodeResult.Unwrap();

        // Define initial entries
        var entries = new[]
        {
            new BlackboardEntry<SensorKey, SensorData>(
                SensorKey.Temperature,
                new SensorData { Value = 20.0, Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), Quality = 100 }),
            new BlackboardEntry<SensorKey, SensorData>(
                SensorKey.Humidity,
                new SensorData { Value = 50.0, Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), Quality = 100 }),
            new BlackboardEntry<SensorKey, SensorData>(
                SensorKey.Pressure,
                new SensorData { Value = 1013.25, Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), Quality = 100 })
        };

        // Create blackboard service with key comparer
        var serviceResult = node.ServiceBuilder()
            .Blackboard<SensorKey>((a, b) => a == b)
            .Create(SERVICE_NAME, entries);

        if (!serviceResult.IsOk)
        {
            Console.WriteLine($"Failed to create blackboard service: {serviceResult}");
            return;
        }

        using var service = serviceResult.Unwrap();

        // Create writer
        var writerResult = service.CreateWriter();
        if (!writerResult.IsOk)
        {
            Console.WriteLine($"Failed to create writer: {writerResult}");
            return;
        }

        using var writer = writerResult.Unwrap();

        Console.WriteLine("Blackboard created. Writing sensor data...");
        Console.WriteLine("Press Ctrl+C to stop.");

        var random = new Random();
        int iteration = 0;

        // Main loop - update sensor values using node.Wait() for proper signal handling
        while (node.Wait(TimeSpan.FromSeconds(1)) == NodeWaitResult.Ok)
        {
            iteration++;

            // Update temperature sensor
            {
                var entryResult = writer.Entry<SensorData>(SensorKey.Temperature);
                if (entryResult.IsOk)
                {
                    using var entry = entryResult.Unwrap();
                    var data = new SensorData
                    {
                        Value = 20.0 + random.NextDouble() * 10.0, // 20-30 degrees
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        Quality = 100
                    };
                    var updateResult = entry.Update(data);
                    if (updateResult.IsOk)
                    {
                        Console.WriteLine($"[{iteration}] Temperature: {data.Value:F2}C");
                    }
                }
            }

            // Update humidity sensor
            {
                var entryResult = writer.Entry<SensorData>(SensorKey.Humidity);
                if (entryResult.IsOk)
                {
                    using var entry = entryResult.Unwrap();
                    var data = new SensorData
                    {
                        Value = 40.0 + random.NextDouble() * 40.0, // 40-80%
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        Quality = 95
                    };
                    var updateResult = entry.Update(data);
                    if (updateResult.IsOk)
                    {
                        Console.WriteLine($"[{iteration}] Humidity: {data.Value:F2}%");
                    }
                }
            }

            // Update pressure sensor
            {
                var entryResult = writer.Entry<SensorData>(SensorKey.Pressure);
                if (entryResult.IsOk)
                {
                    using var entry = entryResult.Unwrap();
                    var data = new SensorData
                    {
                        Value = 1010.0 + random.NextDouble() * 20.0, // 1010-1030 hPa
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        Quality = 98
                    };
                    var updateResult = entry.Update(data);
                    if (updateResult.IsOk)
                    {
                        Console.WriteLine($"[{iteration}] Pressure: {data.Value:F2} hPa");
                    }
                }
            }

            Console.WriteLine();
        }

        Console.WriteLine("Shutting down creator...");
    }

    static void RunOpener()
    {
        Console.WriteLine("Starting blackboard opener...");

        // Create node
        var nodeResult = NodeBuilder.New()
            .Name("blackboard_opener")
            .Create();

        if (!nodeResult.IsOk)
        {
            Console.WriteLine($"Failed to create node: {nodeResult}");
            return;
        }

        using var node = nodeResult.Unwrap();

        // Open blackboard service
        var serviceResult = node.ServiceBuilder()
            .Blackboard<SensorKey>((a, b) => a == b)
            .Open(SERVICE_NAME);

        if (!serviceResult.IsOk)
        {
            Console.WriteLine($"Failed to open blackboard service: {serviceResult}");
            Console.WriteLine("Make sure the creator is running first.");
            return;
        }

        using var service = serviceResult.Unwrap();

        // Create reader
        var readerResult = service.CreateReader();
        if (!readerResult.IsOk)
        {
            Console.WriteLine($"Failed to create reader: {readerResult}");
            return;
        }

        using var reader = readerResult.Unwrap();

        Console.WriteLine("Blackboard opened. Reading sensor data...");
        Console.WriteLine("Press Ctrl+C to stop.");

        int iteration = 0;

        // Main loop - read sensor values using node.Wait() for proper signal handling
        while (node.Wait(TimeSpan.FromMilliseconds(500)) == NodeWaitResult.Ok)
        {
            iteration++;

            Console.WriteLine($"--- Reading #{iteration} ---");

            // Read temperature
            {
                var entryResult = reader.Entry<SensorData>(SensorKey.Temperature);
                if (entryResult.IsOk)
                {
                    using var entry = entryResult.Unwrap();
                    var valueResult = entry.Get();
                    if (valueResult.IsOk)
                    {
                        var data = valueResult.Unwrap();
                        Console.WriteLine($"  Temperature: {data.Value:F2}C (quality: {data.Quality})");
                    }
                }
            }

            // Read humidity
            {
                var entryResult = reader.Entry<SensorData>(SensorKey.Humidity);
                if (entryResult.IsOk)
                {
                    using var entry = entryResult.Unwrap();
                    var valueResult = entry.Get();
                    if (valueResult.IsOk)
                    {
                        var data = valueResult.Unwrap();
                        Console.WriteLine($"  Humidity: {data.Value:F2}% (quality: {data.Quality})");
                    }
                }
            }

            // Read pressure
            {
                var entryResult = reader.Entry<SensorData>(SensorKey.Pressure);
                if (entryResult.IsOk)
                {
                    using var entry = entryResult.Unwrap();
                    var valueResult = entry.Get();
                    if (valueResult.IsOk)
                    {
                        var data = valueResult.Unwrap();
                        Console.WriteLine($"  Pressure: {data.Value:F2} hPa (quality: {data.Quality})");
                    }
                }
            }

            Console.WriteLine();
        }

        Console.WriteLine("Shutting down opener...");
    }
}