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

using System;
using System.Threading;
using Iceoryx2;

namespace EventExample;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: Event <notifier|listener>");
            return;
        }

        var mode = args[0].ToLower();

        if (mode == "notifier")
        {
            RunNotifier();
        }
        else if (mode == "listener")
        {
            RunListener();
        }
        else
        {
            Console.WriteLine("Invalid mode. Use 'notifier' or 'listener'");
        }
    }

    static void RunNotifier()
    {
        Console.WriteLine("Starting notifier...");

        // Create node
        var nodeResult = NodeBuilder.New()
            .Name("notifier_node")
            .Create();

        if (!nodeResult.IsOk)
        {
            Console.WriteLine($"Failed to create node: {nodeResult}");
            return;
        }

        using var node = nodeResult.Unwrap();

        // Open or create event service
        var serviceResult = node.ServiceBuilder()
            .Event()
            .Open("MyEventService");

        if (!serviceResult.IsOk)
        {
            Console.WriteLine($"Failed to create service: {serviceResult}");
            return;
        }

        using var service = serviceResult.Unwrap();

        // Create notifier with default event ID
        var notifierResult = service.CreateNotifier(defaultEventId: new EventId(100));

        if (!notifierResult.IsOk)
        {
            Console.WriteLine($"Failed to create notifier: {notifierResult}");
            return;
        }

        using var notifier = notifierResult.Unwrap();

        Console.WriteLine("Notifier connected. Press Ctrl+C to stop.");
        ulong counter = 0;

        while (true)
        {
            counter++;
            var eventId = new EventId(counter % 12);

            var notifyResult = notifier.Notify(eventId);

            if (!notifyResult.IsOk)
            {
                Console.WriteLine($"Failed to notify: {notifyResult}");
            }
            else
            {
                Console.WriteLine($"Triggered event with id {eventId.Value} ...");
            }

            Thread.Sleep(1000);
        }
    }

    static void RunListener()
    {
        Console.WriteLine("Starting listener...");

        // Create node
        var nodeResult = NodeBuilder.New()
            .Name("listener_node")
            .Create();

        if (!nodeResult.IsOk)
        {
            Console.WriteLine($"Failed to create node: {nodeResult}");
            return;
        }

        using var node = nodeResult.Unwrap();

        // Open event service
        var serviceResult = node.ServiceBuilder()
            .Event()
            .Open("MyEventService");

        if (!serviceResult.IsOk)
        {
            Console.WriteLine($"Failed to open service: {serviceResult}");
            return;
        }

        using var service = serviceResult.Unwrap();

        // Create listener
        var listenerResult = service.CreateListener();

        if (!listenerResult.IsOk)
        {
            Console.WriteLine($"Failed to create listener: {listenerResult}");
            return;
        }

        using var listener = listenerResult.Unwrap();

        Console.WriteLine("Listener ready to receive events!");

        while (true)
        {
            // Wait for event with 1 second timeout
            var waitResult = listener.TimedWait(TimeSpan.FromSeconds(1));

            if (!waitResult.IsOk)
            {
                Console.WriteLine($"Wait failed: {waitResult}");
                break;
            }

            var receivedEventId = waitResult.Unwrap();
            if (receivedEventId.HasValue)
            {
                Console.WriteLine($"Event was triggered with id: {receivedEventId.Value.Value}");
            }
            // If null, timeout - continue waiting
        }
    }
}
