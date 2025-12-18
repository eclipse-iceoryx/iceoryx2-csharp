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

namespace PublishSubscribeExample;

/// <summary>
/// Simple publish-subscribe example demonstrating zero-copy IPC in C#.
/// This example mirrors the Rust/C++/Python examples.
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("iceoryx2 C# Publish-Subscribe Example");
        Console.WriteLine("======================================\n");

        if (args.Length == 0)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run publisher   - Run as publisher");
            Console.WriteLine("  dotnet run subscriber  - Run as subscriber");
            return;
        }

        var mode = args[0].ToLower();
        switch (mode)
        {
            case "publisher":
                RunPublisher();
                break;
            case "subscriber":
                RunSubscriber();
                break;
            default:
                Console.WriteLine($"Unknown mode: {mode}");
                break;
        }
    }

    static void RunPublisher()
    {
        Console.WriteLine("Starting Publisher...\n");

        // Create a node
        using var node = NodeBuilder.New()
            .Name("csharp_publisher")
            .Create()
            .Expect("Failed to create node");

        Console.WriteLine($"Node created: {node.Name}");

        // Open or create a service
        using var service = node.ServiceBuilder()
            .PublishSubscribe<int>()
            .Open("MyService")
            .Expect("Failed to open service");

        Console.WriteLine("Service opened");

        // Create a publisher
        using var publisher = service.CreatePublisher()
            .Expect("Failed to create publisher");

        Console.WriteLine("Publisher created\n");

        // Publish data
        var counter = 0;
        while (true)
        {
            var sample = publisher.Loan<int>()
                .Expect("Failed to loan sample");

            sample.Payload = counter;

            sample.Send()
                .Expect("Failed to send sample");

            Console.WriteLine($"Sent: {counter}");

            counter++;
            Thread.Sleep(1000);
        }
    }

    static void RunSubscriber()
    {
        Console.WriteLine("Starting Subscriber...\n");

        // Create a node
        using var node = NodeBuilder.New()
            .Name("csharp_subscriber")
            .Create()
            .Expect("Failed to create node");

        Console.WriteLine($"Node created: {node.Name}");

        // Open the service
        using var service = node.ServiceBuilder()
            .PublishSubscribe<int>()
            .Open("MyService")
            .Expect("Failed to open service");

        Console.WriteLine("Service opened");

        // Create a subscriber
        using var subscriber = service.SubscriberBuilder()
            .Create()
            .Expect("Failed to create subscriber");

        Console.WriteLine("Subscriber created\n");
        Console.WriteLine("Waiting for samples...\n");

        // Receive data
        while (true)
        {
            var receiveResult = subscriber.Receive<int>();

            if (!receiveResult.IsOk)
            {
                Console.WriteLine($"Error receiving: {receiveResult}");
                break;
            }

            var sampleResult = receiveResult.Unwrap();

            if (sampleResult != null)
            {
                using var sample = sampleResult;
                Console.WriteLine($"Received: {sample.Payload}");
            }
            else
            {
                // No sample available yet
                Console.Write(".");
            }

            Thread.Sleep(100);
        }
    }
}