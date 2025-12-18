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

namespace RuntimeTest;

class Program
{
    static int Main(string[] args)
    {
        Console.WriteLine("iceoryx2 C# Runtime Test");
        Console.WriteLine("=========================\n");

        try
        {
            // Test 1: Create a node
            Console.WriteLine("Test 1: Creating a node...");
            Console.WriteLine("  Step 1: Calling NodeBuilder.New()");
            var builder = NodeBuilder.New();

            Console.WriteLine("  Step 2: Calling Create() WITHOUT name");
            var nodeResult = builder.Create();

            Console.WriteLine($"  Step 3: Checking result. IsOk = {nodeResult.IsOk}");
            if (!nodeResult.IsOk)
            {
                Console.WriteLine($"❌ Failed to create node: {nodeResult}");
                return 1;
            }

            Console.WriteLine("  Step 4: Unwrapping node");
            using var node = nodeResult.Unwrap();
            Console.WriteLine($"✓ Node created successfully");
            Console.WriteLine($"  Name: {node.Name}");
            Console.WriteLine($"  ID: {node.Id}");
            Console.WriteLine();

            // Test 2: Create a service
            Console.WriteLine("Test 2: Creating a service...");
            var serviceResult = node.ServiceBuilder()
                .PublishSubscribe<int>()
                .Open("TestService");

            if (!serviceResult.IsOk)
            {
                Console.WriteLine($"❌ Failed to create service: {serviceResult}");
                return 1;
            }

            using var service = serviceResult.Unwrap();
            Console.WriteLine("✓ Service created successfully");
            Console.WriteLine();

            // Test 3: Create a publisher
            Console.WriteLine("Test 3: Creating a publisher...");
            var publisherResult = service.CreatePublisher();

            if (!publisherResult.IsOk)
            {
                Console.WriteLine($"❌ Failed to create publisher: {publisherResult}");
                return 1;
            }

            using var publisher = publisherResult.Unwrap();
            Console.WriteLine("✓ Publisher created successfully");
            Console.WriteLine();

            // Test 4: Send a sample
            Console.WriteLine("Test 4: Sending a sample...");
            var sampleResult = publisher.Loan<int>();

            if (!sampleResult.IsOk)
            {
                Console.WriteLine($"❌ Failed to loan sample: {sampleResult}");
                return 1;
            }

            var sample = sampleResult.Unwrap();
            sample.Payload = 42;

            var sendResult = sample.Send();
            if (!sendResult.IsOk)
            {
                Console.WriteLine($"❌ Failed to send sample: {sendResult}");
                return 1;
            }

            Console.WriteLine("✓ Sample sent successfully (payload: 42)");
            Console.WriteLine();

            // Test 5: Create a subscriber
            Console.WriteLine("Test 5: Creating a subscriber...");
            var subscriberResult = service.CreateSubscriber();

            if (!subscriberResult.IsOk)
            {
                Console.WriteLine($"❌ Failed to create subscriber: {subscriberResult}");
                return 1;
            }

            using var subscriber = subscriberResult.Unwrap();
            Console.WriteLine("✓ Subscriber created successfully");
            Console.WriteLine();

            Console.WriteLine("============================");
            Console.WriteLine("✓ All tests passed!");
            Console.WriteLine("============================");

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Exception occurred: {ex.Message}");
            Console.WriteLine($"Stack trace:\n{ex.StackTrace}");
            return 1;
        }
    }
}