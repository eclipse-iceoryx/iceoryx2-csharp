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
using System.Threading.Tasks;

namespace RequestResponseExample;

/// <summary>
/// Async/await version of the Request-Response example demonstrating modern C# async patterns
/// </summary>
class ProgramAsync
{
    public static async Task RunClientAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Starting async client...");

        // Create node
        var nodeResult = NodeBuilder.New()
            .Name("request_response_async_client")
            .Create();

        if (!nodeResult.IsOk)
        {
            Console.WriteLine($"Failed to create node: {nodeResult}");
            return;
        }

        using var node = nodeResult.Unwrap();

        // Open or create request-response service
        var serviceResult = node.ServiceBuilder()
            .RequestResponse<ulong, TransmissionData>()
            .Open("My/Funk/ServiceName");

        if (!serviceResult.IsOk)
        {
            Console.WriteLine($"Failed to open service: {serviceResult}");
            return;
        }

        using var service = serviceResult.Unwrap();

        // Create client
        var clientResult = service.CreateClient();
        if (!clientResult.IsOk)
        {
            Console.WriteLine($"Failed to create client: {clientResult}");
            return;
        }

        using var client = clientResult.Unwrap();

        Console.WriteLine("Async client started. Sending requests...");

        ulong requestCounter = 0;
        ulong responseCounter = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine($"Sending request {requestCounter}...");

            // Send request
            var sendResult = client.SendCopy(requestCounter);
            if (!sendResult.IsOk)
            {
                Console.WriteLine($"Failed to send request: {sendResult}");
                return;
            }

            using var pendingResponse = sendResult.Unwrap();

            // Wait for response asynchronously with 2-second timeout
            var responseResult = await pendingResponse.ReceiveAsync(
                TimeSpan.FromSeconds(2),
                cancellationToken);

            if (!responseResult.IsOk)
            {
                Console.WriteLine($"Failed to receive response: {responseResult}");
                return;
            }

            var response = responseResult.Unwrap();
            if (response == null)
            {
                Console.WriteLine("  Request timed out (no response within 2 seconds)");
            }
            else
            {
                using (response)
                {
                    Console.WriteLine($"  Received response {responseCounter}: x={response.Payload.X}, y={response.Payload.Y}, funky={response.Payload.Funky:F2}");
                    responseCounter++;
                }
            }

            requestCounter++;

            // Wait 1 second between requests (non-blocking)
            await Task.Delay(1000, cancellationToken);
        }
    }

    public static async Task RunServerAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Starting async server...");

        // Create node
        var nodeResult = NodeBuilder.New()
            .Name("request_response_async_server")
            .Create();

        if (!nodeResult.IsOk)
        {
            Console.WriteLine($"Failed to create node: {nodeResult}");
            return;
        }

        using var node = nodeResult.Unwrap();

        // Open or create request-response service
        var serviceResult = node.ServiceBuilder()
            .RequestResponse<ulong, TransmissionData>()
            .Open("My/Funk/ServiceName");

        if (!serviceResult.IsOk)
        {
            Console.WriteLine($"Failed to open service: {serviceResult}");
            return;
        }

        using var service = serviceResult.Unwrap();

        // Create server
        var serverResult = service.CreateServer();
        if (!serverResult.IsOk)
        {
            Console.WriteLine($"Failed to create server: {serverResult}");
            return;
        }

        using var server = serverResult.Unwrap();

        Console.WriteLine("Async server ready to receive requests!");

        int counter = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            // Receive requests (non-blocking)
            var receiveResult = server.Receive();
            if (!receiveResult.IsOk)
            {
                Console.WriteLine($"Failed to receive request: {receiveResult}");
                return;
            }

            var request = receiveResult.Unwrap();
            if (request == null)
            {
                // No request available, yield to thread pool
                await Task.Delay(10, cancellationToken);
                continue;
            }

            using (request)
            {
                ulong requestValue = request.Payload;
                Console.WriteLine($"Received request: {requestValue}");

                // Create response data
                var response = new TransmissionData
                {
                    X = 5 + counter,
                    Y = 6 * counter,
                    Funky = 7.77
                };

                Console.WriteLine($"  Sending response: x={response.X}, y={response.Y}, funky={response.Funky:F2}");

                // Send response
                var sendResult = request.SendCopyResponse(response);
                if (!sendResult.IsOk)
                {
                    Console.WriteLine($"Failed to send response: {sendResult}");
                    continue;
                }

                counter++;
            }
        }
    }

    // Example of how to use this in a Main method:
    // static async Task Main(string[] args)
    // {
    //     var cts = new CancellationTokenSource();
    //     Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };
    //
    //     if (args.Length > 0 && args[0] == "client")
    //     {
    //         await RunClientAsync(cts.Token);
    //     }
    //     else
    //     {
    //         await RunServerAsync(cts.Token);
    //     }
    // }
}