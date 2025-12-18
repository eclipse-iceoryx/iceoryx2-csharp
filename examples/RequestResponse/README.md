# Request-Response Example in C Sharp

> [!CAUTION]
> Every payload you transmit with iceoryx2 must be compatible with shared
> memory. Specifically, it must:
>
> * be self contained, no heap, no pointers to external sources
> * have a uniform memory representation, ensuring that shared structs have the
>     same data layout
> * not use pointers to manage their internal structure
>
> **Use `[StructLayout(LayoutKind.Sequential)]` for complex types to ensure**
> **cross-platform and cross-language compatibility!**

This example demonstrates the request-response messaging pattern between two
separate processes using iceoryx2. A key feature of request-response in
iceoryx2 is that the `Client` can receive a stream of responses instead of
being limited to just one.

## Client Side

The `Client` uses the following approach:

1. Sends first request by using the slower copy API (`SendCopy()`) and then
   enters a loop.
2. Inside the loop: Loans memory and acquires a `RequestMut`.
3. Writes the payload into the `RequestMut`.
4. Sends the `RequestMut` to the `Server` and receives a `PendingResponse`
   object. The `PendingResponse` can be used to:
   * Receive `Response`s for this specific `RequestMut`.
   * Signal the `Server` that the `Client` is no longer interested in data by
     being disposed.
   * Check whether the corresponding active request on the `Server` side is
     still connected.

## Server Side

The `Server` uses the following approach:

1. Receives the request sent by the `Client` and obtains a `Request` object.
2. The `Request` can be used to:
   * Read the payload via the `Payload` property.
   * Loan memory for a `ResponseMut` using `LoanResponse()`.
   * Signal the `Client` that it is no longer sending responses by being
     disposed.
   * Check whether the corresponding `PendingResponse` on the `Client` side
     is still connected.
3. Sends one `Response` by using the slower copy API (`SendCopyResponse()`).
4. Loans memory via the `Request` for a `ResponseMut` to send additional responses.

Sending multiple responses demonstrates the streaming API. The `Request`
and the `PendingResponse` are connected - as soon as either is disposed,
further communication between them is no longer possible.

In this example, both the client and server print the received and sent data
to the console.

## How to Build

Before proceeding, ensure you have:

* .NET 8.0 SDK or later
* The iceoryx2 C FFI library built (`cargo build --release --package iceoryx2-ffi-c`)

Build the example:

```sh
cd iceoryx2-ffi/csharp/examples/RequestResponse
dotnet build
```

## How to Run

To observe the communication in action, open two terminals and execute the
following commands:

### Terminal 1 (Server)

```sh
dotnet run -- server
```

### Terminal 2 (Client)

```sh
dotnet run -- client
```

Feel free to run multiple instances of the client or server processes
simultaneously to explore how iceoryx2 handles request-response communication
efficiently.

> [!TIP]
> You may hit the maximum supported number of ports when too many client or
> server processes are running. Refer to the [iceoryx2 config](../../../../config)
> to configure limits globally, or use the Service builder API to set them
> for a specific service.

## Example Output

### Server Output

```text
Starting server...
Server ready to receive requests!
received request: 0
  send response: x=5, y=0, funky=7.77
received request: 1
  send response: x=6, y=6, funky=7.77
  send response: x=1, y=1, funky=0.1234
received request: 2
  send response: x=7, y=12, funky=7.77
```

### Client Output

```text
Starting client...
Client started. Sending requests...
send request 0 ...
  received response 0: x=5, y=0, funky=7.77
send request 1 ...
  received response 1: x=6, y=6, funky=7.77
  received response 2: x=1, y=1, funky=0.12
send request 2 ...
  received response 3: x=7, y=12, funky=7.77
```

## Key Features Demonstrated

* **Request-Response Pattern**: Client-server RPC communication
* **Streaming Responses**: Server can send multiple responses per request
* **Zero-Copy API**: Using `Loan()` for efficient memory sharing
* **Copy API**: Using `SendCopy()` and `SendCopyResponse()` for convenience
* **Resource Management**: Proper disposal of requests, responses, and pending responses
* **Type Safety**: Generic `RequestResponse<TRequest, TResponse>` with compile-
  time checks
* **Cross-Language Compatible**: Uses `ulong` (u64) and `TransmissionData`
  struct compatible with C/C++/Rust
