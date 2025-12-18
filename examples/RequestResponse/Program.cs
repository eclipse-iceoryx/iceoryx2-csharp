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
using Iceoryx2.RequestResponse;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace RequestResponseExample;

/// <summary>
/// Response payload matching the C example's TransmissionData
/// </summary>
[StructLayout(LayoutKind.Sequential)]
struct TransmissionData
{
    public int X;
    public int Y;
    public double Funky;
}

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0 || (args[0] != "client" && args[0] != "server"))
        {
            Console.WriteLine("Usage: RequestResponse [client|server]");
            Console.WriteLine("");
            Console.WriteLine("  client - Send requests and receive responses");
            Console.WriteLine("  server - Receive requests and send responses");
            return;
        }

        if (args[0] == "client")
        {
            RunClient();
        }
        else
        {
            RunServer();
        }
    }

    static void RunClient()
    {
        Console.WriteLine("Starting client...");

        // Create node
        var nodeResult = NodeBuilder.New()
            .Name("request_response_client")
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

        Console.WriteLine("Client started. Sending requests...");

        ulong requestCounter = 0;
        ulong responseCounter = 0;
        PendingResponse<TransmissionData>? pendingResponse = null;

        try
        {
            // For the first request, use the copy API
            Console.WriteLine($"send request {requestCounter} ...");
            var sendResult = client.SendCopy(requestCounter);
            if (!sendResult.IsOk)
            {
                Console.WriteLine($"Failed to send initial request: {sendResult}");
                return;
            }

            pendingResponse = sendResult.Unwrap();

            // Main loop - send requests and receive responses
            while (true)
            {
                // Give server time to process
                Thread.Sleep(100);

                Console.WriteLine("DEBUG: Checking for responses...");

                // Check for responses
                while (true)
                {
                    var responseResult = pendingResponse.TryReceive();
                    if (!responseResult.IsOk)
                    {
                        Console.WriteLine($"Failed to receive response: {responseResult}");
                        return;
                    }

                    var response = responseResult.Unwrap();
                    if (response == null)
                    {
                        Console.WriteLine("DEBUG: No response available yet");
                        break; // No more responses available
                    }

                    using (response)
                    {
                        Console.WriteLine($"  received response {responseCounter}: x={response.Payload.X}, y={response.Payload.Y}, funky={response.Payload.Funky:F2}");
                        responseCounter++;
                    }
                }

                requestCounter++;

                // Dispose previous pending response
                pendingResponse?.Dispose();
                pendingResponse = null;

                // For subsequent requests, use the zero-copy API
                Console.WriteLine($"send request {requestCounter} ...");

                // Loan request sample
                var loanResult = client.Loan();
                if (!loanResult.IsOk)
                {
                    Console.WriteLine($"Failed to loan request: {loanResult}");
                    return;
                }

                using var request = loanResult.Unwrap();

                // Write payload
                request.Payload = requestCounter;

                // Send request
                var sendZeroCopyResult = request.Send();
                if (!sendZeroCopyResult.IsOk)
                {
                    Console.WriteLine($"Failed to send request: {sendZeroCopyResult}");
                    return;
                }

                pendingResponse = sendZeroCopyResult.Unwrap();

                // Wait 1 second between requests
                Thread.Sleep(1000);
            }
        }
        finally
        {
            pendingResponse?.Dispose();
        }
    }

    static void RunServer()
    {
        Console.WriteLine("Starting server...");

        // Create node
        var nodeResult = NodeBuilder.New()
            .Name("request_response_server")
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

        Console.WriteLine("Server ready to receive requests!");

        int counter = 0;

        // Main loop
        while (true)
        {
            // Receive requests
            while (true)
            {
                var receiveResult = server.Receive();
                if (!receiveResult.IsOk)
                {
                    Console.WriteLine($"Failed to receive request: {receiveResult}");
                    return;
                }

                var request = receiveResult.Unwrap();
                if (request == null)
                {
                    break; // No more requests available
                }

                using (request)
                {
                    ulong requestValue = request.Payload;
                    Console.WriteLine($"received request: {requestValue}");

                    // Create response data
                    var response = new TransmissionData
                    {
                        X = 5 + counter,
                        Y = 6 * counter,
                        Funky = 7.77
                    };

                    Console.WriteLine($"  send response: x={response.X}, y={response.Y}, funky={response.Funky:F2}");

                    // Send first response using copy API
                    var sendResult = request.SendCopyResponse(response);
                    if (!sendResult.IsOk)
                    {
                        Console.WriteLine($"Failed to send response: {sendResult}");
                        continue;
                    }

                    // // Optionally send additional responses using zero-copy API
                    // // (mimicking the C example's behavior based on request value % 2)
                    // for (int iter = 0; iter < (int)(requestValue % 2); iter++)
                    // {
                    //     var loanResult = request.LoanResponse();
                    //     if (!loanResult.IsOk)
                    //     {
                    //         Console.WriteLine($"Failed to loan response sample: {loanResult}");
                    //         continue;
                    //     }

                    //     using var responseMut = loanResult.Unwrap();

                    //     // Write payload
                    //     responseMut.Payload = new TransmissionData
                    //     {
                    //         X = counter * (iter + 1),
                    //         Y = counter + iter,
                    //         Funky = counter * 0.1234
                    //     };

                    //     Console.WriteLine($"  send response: x={responseMut.Payload.X}, y={responseMut.Payload.Y}, funky={responseMut.Payload.Funky:F4}");

                    //     // Send response
                    //     var sendZeroCopyResult = responseMut.Send();
                    //     if (!sendZeroCopyResult.IsOk)
                    //     {
                    //         Console.WriteLine($"Failed to send additional response: {sendZeroCopyResult}");
                    //     }
                    // }
                }
            }

            counter++;

            // Sleep 100ms between cycles
            Thread.Sleep(100);
        }
    }
}