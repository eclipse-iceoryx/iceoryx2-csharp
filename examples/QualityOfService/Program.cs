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

/// <summary>
/// Demonstrates Quality of Service (QoS) settings in iceoryx2.
/// 
/// QoS settings allow you to configure:
/// - Maximum number of subscribers and publishers
/// - Buffer sizes for subscribers
/// - History size for late-joining subscribers
/// - Safe overflow behavior (overwrite oldest vs block)
/// - Maximum loaned samples for publishers
/// </summary>
class Program
{
    static void Main()
    {
        Console.WriteLine("=== iceoryx2 Quality of Service (QoS) Example ===\n");

        // Create a node
        var node = NodeBuilder.New()
            .Name("qos_example_node")
            .Create()
            .Expect("Failed to create node");

        Console.WriteLine("✓ Node created\n");

        // ========================================
        // Example 1: Service-Level QoS Settings
        // ========================================
        Console.WriteLine("--- Example 1: Service-Level QoS Settings ---");

        var service = node.ServiceBuilder()
            .PublishSubscribe<ulong>()
            .MaxSubscribers(5)                      // Allow up to 5 subscribers
            .MaxPublishers(2)                       // Allow up to 2 publishers
            .SubscriberMaxBufferSize(10)            // Each subscriber can buffer 10 samples
            .SubscriberMaxBorrowedSamples(3)        // Each subscriber can borrow 3 samples at once
            .HistorySize(5)                         // Keep last 5 samples for late-joiners
            .EnableSafeOverflow(true)               // Overwrite oldest when buffer is full
            .Open("qos_demo_service")
            .Expect("Failed to create service");

        Console.WriteLine("✓ Service created with QoS settings:");
        Console.WriteLine("  - Max subscribers: 5");
        Console.WriteLine("  - Max publishers: 2");
        Console.WriteLine("  - Subscriber buffer size: 10");
        Console.WriteLine("  - Subscriber max borrowed: 3");
        Console.WriteLine("  - History size: 5");
        Console.WriteLine("  - Safe overflow: enabled\n");

        // ========================================
        // Example 2: Publisher-Level QoS Settings
        // ========================================
        Console.WriteLine("--- Example 2: Publisher-Level QoS Settings ---");

        // Create publisher with custom max loaned samples
        var publisher = service.PublisherBuilder()
            .MaxLoanedSamples(5)    // Publisher can loan up to 5 samples at once
            .Create()
            .Expect("Failed to create publisher");

        Console.WriteLine("✓ Publisher created with QoS settings:");
        Console.WriteLine("  - Max loaned samples: 5\n");

        // ========================================
        // Example 3: Late-Joiner History
        // ========================================
        Console.WriteLine("--- Example 3: Demonstrating History Size ---");

        // Send some samples before subscriber connects
        Console.WriteLine("Publishing 5 samples before subscriber connects...");
        for (ulong i = 1; i <= 5; i++)
        {
            publisher.SendCopy(i).Expect($"Failed to send {i}");
            Console.WriteLine($"  Published: {i}");
        }

        // Now create a subscriber - it should receive the last 5 samples (history)
        Console.WriteLine("\nCreating late-joining subscriber...");
        Console.WriteLine("Note: Subscriber must request buffer size >= history size to receive historical samples");
        var subscriber = service.SubscriberBuilder()
            .BufferSize(10)  // Request buffer size >= history size (5) to receive history
            .Create()
            .Expect("Failed to create subscriber");

        Console.WriteLine("✓ Subscriber created with buffer size 10");

        // CRITICAL: Publisher must explicitly update connections to deliver history
        Console.WriteLine("✓ Updating publisher connections to deliver history...");
        publisher.UpdateConnections().Expect("Failed to update connections");

        Console.WriteLine("\nReceiving historical samples:");

        // Receive the historical samples
        for (int i = 0; i < 5; i++)
        {
            var sample = subscriber.Receive<ulong>().Expect("Failed to receive");
            if (sample != null)
            {
                using (sample)
                {
                    Console.WriteLine($"  Received historical sample: {sample.Payload}");
                }
            }
        }

        // ========================================
        // Example 4: Safe Overflow Behavior
        // ========================================
        Console.WriteLine("\n--- Example 4: Demonstrating Safe Overflow ---");
        Console.WriteLine("Buffer size is 10, sending 15 samples to trigger overflow...");

        for (ulong i = 10; i < 25; i++)
        {
            publisher.SendCopy(i).Expect($"Failed to send {i}");
            Console.WriteLine($"  Published: {i}");
        }

        Console.WriteLine("\nReceiving samples (oldest 5 should be overwritten):");

        int received = 0;
        while (received < 10) // Buffer size is 10
        {
            var sample = subscriber.Receive<ulong>().Expect("Failed to receive");
            if (sample != null)
            {
                using (sample)
                {
                    Console.WriteLine($"  Received: {sample.Payload}");
                    received++;
                }
            }
            else
            {
                break; // No more samples
            }
        }

        Console.WriteLine("\n=== QoS Example Complete ===");
        Console.WriteLine("\nKey QoS Settings Summary:");
        Console.WriteLine("┌─────────────────────────────────┬──────────┬────────────────────────────────┐");
        Console.WriteLine("│ QoS Setting                     │ Level    │ Description                    │");
        Console.WriteLine("├─────────────────────────────────┼──────────┼────────────────────────────────┤");
        Console.WriteLine("│ MaxSubscribers                  │ Service  │ Max concurrent subscribers     │");
        Console.WriteLine("│ MaxPublishers                   │ Service  │ Max concurrent publishers      │");
        Console.WriteLine("│ SubscriberMaxBufferSize         │ Service  │ Samples per subscriber buffer  │");
        Console.WriteLine("│ SubscriberMaxBorrowedSamples    │ Service  │ Concurrent borrows allowed     │");
        Console.WriteLine("│ HistorySize                     │ Service  │ Samples for late-joiners       │");
        Console.WriteLine("│ EnableSafeOverflow              │ Service  │ Overwrite vs block behavior    │");
        Console.WriteLine("│ MaxLoanedSamples                │ Publisher│ Concurrent loans allowed       │");
        Console.WriteLine("└─────────────────────────────────┴──────────┴────────────────────────────────┘");
    }
}