// Copyright (c) 2024 Contributors to the Eclipse Foundation
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

// TODO(@patdhlk): cleanup example
class CallbackContext : IDisposable
{
    public WaitSetGuard[] Guards { get; set; } = Array.Empty<WaitSetGuard>();
    public Listener[] Listeners { get; set; } = Array.Empty<Listener>();
    public string[] ServiceNames { get; set; } = Array.Empty<string>();

    public void Dispose()
    {
        foreach (var guard in Guards)
        {
            guard.Dispose();
        }
        foreach (var listener in Listeners)
        {
            listener.Dispose();
        }
    }
}

class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run wait SERVICE_NAME_1 SERVICE_NAME_2");
            Console.WriteLine("  dotnet run notify EVENT_ID SERVICE_NAME");
            return -1;
        }

        var command = args[0];

        if (command == "wait")
        {
            return await RunWaiterAsync(args.Skip(1).ToArray());
        }
        else if (command == "notify")
        {
            return await RunNotifierAsync(args.Skip(1).ToArray());
        }
        else
        {
            Console.WriteLine($"Unknown command: {command}");
            Console.WriteLine("Valid commands: wait, notify");
            return -1;
        }
    }

    static async Task<int> RunWaiterAsync(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: dotnet run wait SERVICE_NAME_1 SERVICE_NAME_2");
            return -1;
        }

        var serviceName1 = args[0];
        var serviceName2 = args[1];

        // Create node
        var node = NodeBuilder.New().Create().Expect("Failed to create node");

        // Create first event service
        var service1 = node.ServiceBuilder()
            .Event()
            .Open(serviceName1)
            .Expect($"Failed to create service '{serviceName1}'");

        // Create second event service
        var service2 = node.ServiceBuilder()
            .Event()
            .Open(serviceName2)
            .Expect($"Failed to create service '{serviceName2}'");

        // Create listeners
        var listener1 = service1.CreateListener()
            .Expect($"Failed to create listener for '{serviceName1}'");

        var listener2 = service2.CreateListener()
            .Expect($"Failed to create listener for '{serviceName2}'");

        // Create WaitSet with signal handling for graceful shutdown
        var waitset = WaitSetBuilder.New()
            .SignalHandling(SignalHandlingMode.TerminationAndInterrupt)
            .Create()
            .Expect("Failed to create WaitSet");

        Console.WriteLine($"Waiting on services: '{serviceName1}' and '{serviceName2}'");

        // Attach listeners to WaitSet
        using var context = new CallbackContext
        {
            Guards = new WaitSetGuard[]
            {
                waitset.AttachNotification(listener1).Expect($"Failed to attach listener for '{serviceName1}'"),
                waitset.AttachNotification(listener2).Expect($"Failed to attach listener for '{serviceName2}'")
            },
            Listeners = new Listener[] { listener1, listener2 },
            ServiceNames = new string[] { serviceName1, serviceName2 }
        };

        // Event processing callback
        CallbackProgression OnEvent(WaitSetAttachmentId attachmentId)
        {
            for (int i = 0; i < context.Guards.Length; i++)
            {
                if (attachmentId.HasEventFrom(context.Guards[i]))
                {
                    // CRITICAL: Consume ALL pending events to avoid busy loop
                    // The WaitSet wakes up when there is pending data. If we don't consume all events,
                    // the file descriptor remains ready and we'll immediately wake again.
                    while (true)
                    {
                        var eventResult = context.Listeners[i].TryWait();
                        if (eventResult.IsOk)
                        {
                            var eventIdOpt = eventResult.Unwrap();
                            if (eventIdOpt.HasValue)
                            {
                                var eventId = eventIdOpt.Value;
                                Console.WriteLine($"[service: '{context.ServiceNames[i]}'] event received with id: {eventId.Value}");
                            }
                            else
                            {
                                break; // No more events available
                            }
                        }
                        else
                        {
                            // Error occurred
                            break;
                        }
                    }

                    break;
                }
            }

            return CallbackProgression.Continue;
        }

        // Run event loop asynchronously in background task
        var waitTask = Task.Run(() => waitset.WaitAndProcess(OnEvent));

        // Wait for completion
        var result = await waitTask;

        Console.WriteLine($"WaitSet completed with result: {result}");

        // Cleanup
        waitset.Dispose();
        listener2.Dispose();
        listener1.Dispose();
        service2.Dispose();
        service1.Dispose();
        node.Dispose();

        return 0;
    }

    static async Task<int> RunNotifierAsync(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: dotnet run notify EVENT_ID SERVICE_NAME");
            return -1;
        }

        if (!ulong.TryParse(args[0], out var eventIdValue))
        {
            Console.WriteLine($"Invalid EVENT_ID: {args[0]}");
            return -1;
        }

        var serviceName = args[1];

        // Create node
        var node = NodeBuilder.New().Create().Expect("Failed to create node");

        // Create event service
        var service = node.ServiceBuilder()
            .Event()
            .Open(serviceName)
            .Expect($"Failed to create service '{serviceName}'");

        // Create notifier
        var notifier = service.CreateNotifier()
            .Expect($"Failed to create notifier for '{serviceName}'");

        Console.WriteLine($"Sending events with ID {eventIdValue} to service '{serviceName}'");

        // Send events periodically until interrupted
        var eventId = new EventId(eventIdValue);
        var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                notifier.Notify(eventId)
                    .Expect("Failed to notify listener");

                Console.WriteLine($"[service: '{serviceName}'] Triggered event with id {eventIdValue}");

                await Task.Delay(1000, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when Ctrl+C is pressed
        }

        Console.WriteLine("\nShutting down...");

        // Cleanup
        notifier.Dispose();
        service.Dispose();
        node.Dispose();

        return 0;
    }
}