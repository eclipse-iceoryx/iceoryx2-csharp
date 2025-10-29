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

namespace ServiceDiscoveryExample;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Iceoryx2 Service Discovery Example ===\n");

        // Create a node
        using var node = NodeBuilder.New()
            .Name("discovery_node")
            .Create()
            .Expect("Failed to create node");

        // List all available services
        var services = node.List()
            .Expect("Failed to list services");

        // Display results
        Console.WriteLine($"Found {services.Count} service(s):\n");

        if (services.Count == 0)
        {
            Console.WriteLine("No services are currently running.");
            Console.WriteLine("\nTo see services in action:");
            Console.WriteLine("1. Start a publisher/subscriber example in another terminal");
            Console.WriteLine("2. Run this discovery example again");
            return;
        }

        // Display each service
        foreach (var service in services)
        {
            Console.WriteLine(new string('=', 60));
            Console.WriteLine($"Service: {service.Name}");
            Console.WriteLine($"ID:      {service.Id}");
            Console.WriteLine($"Pattern: {service.MessagingPattern}");
            Console.WriteLine(new string('-', 60));

            // Display pattern-specific configuration
            switch (service.MessagingPattern)
            {
                case MessagingPattern.PublishSubscribe:
                    DisplayPublishSubscribeConfig(service.PublishSubscribeConfig);
                    break;

                case MessagingPattern.Event:
                    DisplayEventConfig(service.EventConfig);
                    break;

                case MessagingPattern.RequestResponse:
                    DisplayRequestResponseConfig(service.RequestResponseConfig);
                    break;

                case MessagingPattern.Blackboard:
                    DisplayBlackboardConfig(service.BlackboardConfig);
                    break;
            }

            Console.WriteLine();
        }
    }

    static void DisplayPublishSubscribeConfig(PublishSubscribeStaticConfig? config)
    {
        if (config == null) return;

        Console.WriteLine("Publish-Subscribe Configuration:");
        Console.WriteLine($"  Max Publishers:             {config.MaxPublishers}");
        Console.WriteLine($"  Max Subscribers:            {config.MaxSubscribers}");
        Console.WriteLine($"  Max Nodes:                  {config.MaxNodes}");
        Console.WriteLine($"  History Size:               {config.HistorySize}");
        Console.WriteLine($"  Subscriber Max Buffer Size: {config.SubscriberMaxBufferSize}");
        Console.WriteLine($"  Subscriber Max Borrowed:    {config.SubscriberMaxBorrowedSamples}");
        Console.WriteLine($"  Safe Overflow Enabled:      {config.EnableSafeOverflow}");
    }

    static void DisplayEventConfig(EventStaticConfig? config)
    {
        if (config == null) return;

        Console.WriteLine("Event Configuration:");
        Console.WriteLine($"  Max Notifiers:       {config.MaxNotifiers}");
        Console.WriteLine($"  Max Listeners:       {config.MaxListeners}");
        Console.WriteLine($"  Max Nodes:           {config.MaxNodes}");
        Console.WriteLine($"  Event ID Max Value:  {config.EventIdMaxValue}");

        if (config.NotifierDeadEvent.HasValue)
            Console.WriteLine($"  Notifier Dead Event:    {config.NotifierDeadEvent.Value}");
        if (config.NotifierDroppedEvent.HasValue)
            Console.WriteLine($"  Notifier Dropped Event: {config.NotifierDroppedEvent.Value}");
        if (config.NotifierCreatedEvent.HasValue)
            Console.WriteLine($"  Notifier Created Event: {config.NotifierCreatedEvent.Value}");
    }

    static void DisplayRequestResponseConfig(RequestResponseStaticConfig? config)
    {
        if (config == null) return;

        Console.WriteLine("Request-Response Configuration:");
        Console.WriteLine($"  Max Clients:                     {config.MaxClients}");
        Console.WriteLine($"  Max Servers:                     {config.MaxServers}");
        Console.WriteLine($"  Max Nodes:                       {config.MaxNodes}");
        Console.WriteLine($"  Max Active Requests per Client:  {config.MaxActiveRequestsPerClient}");
        Console.WriteLine($"  Max Loaned Requests:             {config.MaxLoanedRequests}");
        Console.WriteLine($"  Max Response Buffer Size:        {config.MaxResponseBufferSize}");
        Console.WriteLine($"  Max Borrowed Responses:          {config.MaxBorrowedResponsesPerPendingResponse}");
        Console.WriteLine($"  Safe Overflow (Requests):        {config.EnableSafeOverflowForRequests}");
        Console.WriteLine($"  Safe Overflow (Responses):       {config.EnableSafeOverflowForResponses}");
        Console.WriteLine($"  Fire-and-Forget Enabled:         {config.EnableFireAndForgetRequests}");
    }

    static void DisplayBlackboardConfig(BlackboardStaticConfig? config)
    {
        if (config == null) return;

        Console.WriteLine("Blackboard Configuration:");
        Console.WriteLine($"  Max Readers: {config.MaxReaders}");
        Console.WriteLine($"  Max Writers: {config.MaxWriters}");
        Console.WriteLine($"  Max Nodes:   {config.MaxNodes}");
    }
}