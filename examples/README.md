# Usage Examples

## Publish-Subscribe Pattern

```csharp
using Iceoryx2;

// Create a node
var nodeResult = NodeBuilder.New()
    .Name("my_node")
    .Create();

if (!nodeResult.IsOk)
{
    Console.WriteLine($"Failed to create node: {nodeResult}");
    return;
}

using var node = nodeResult.Unwrap();

// Open or create a service for pub/sub
var serviceResult = node.ServiceBuilder()
    .PublishSubscribe<int>()
    .Open("MyService");

if (!serviceResult.IsOk)
{
    Console.WriteLine($"Failed to open service: {serviceResult}");
    return;
}

using var service = serviceResult.Unwrap();

// Publisher example
var publisherResult = service.CreatePublisher();
if (!publisherResult.IsOk)
{
    Console.WriteLine($"Failed to create publisher: {publisherResult}");
    return;
}

using var publisher = publisherResult.Unwrap();

var sampleResult = publisher.Loan();
if (!sampleResult.IsOk)
{
    Console.WriteLine($"Failed to loan sample: {sampleResult}");
    return;
}

using var sample = sampleResult.Unwrap();
sample.Payload = 42;

var sendResult = sample.Send();
if (!sendResult.IsOk)
{
    Console.WriteLine($"Failed to send: {sendResult}");
}

// Subscriber example
var subscriberResult = service.CreateSubscriber();
if (!subscriberResult.IsOk)
{
    Console.WriteLine($"Failed to create subscriber: {subscriberResult}");
    return;
}

using var subscriber = subscriberResult.Unwrap();

var receiveResult = subscriber.Receive();
if (!receiveResult.IsOk)
{
    Console.WriteLine($"Failed to receive: {receiveResult}");
    return;
}

var receivedSample = receiveResult.Unwrap();
if (receivedSample != null)
{
    Console.WriteLine($"Received: {receivedSample.Payload}");
}
```

## Zero-Copy Access

The bindings provide true zero-copy access to shared memory through `ref`
returns, eliminating intermediate copies when reading or writing payload data.

### Zero-Copy Write (Publisher)

```csharp
using var publisher = service.CreatePublisher().Unwrap();
using var sample = publisher.Loan<MyData>().Unwrap();

// Direct zero-copy access to shared memory
ref var payload = ref sample.GetPayloadRef();
payload.Field1 = 42;
payload.Field2 = 3.14;

sample.Send();
```

### Zero-Copy Read (Subscriber)

```csharp
using var subscriber = service.CreateSubscriber().Unwrap();
var result = subscriber.Receive<MyData>();

if (result.IsOk)
{
    using var sample = result.Unwrap();
    if (sample != null)
    {
        // Direct zero-copy read from shared memory
        ref readonly var payload = ref sample.GetPayloadRefReadOnly();
        Console.WriteLine($"Field1: {payload.Field1}, Field2: {payload.Field2}");
    }
}
```

### API Methods

**`Sample<T>` provides three ways to access payload data:**

1. **`Payload` property** - Copies data (backward compatible)

   ```csharp
   sample.Payload = new MyData { Field1 = 42 };  // Copy on write
   var data = sample.Payload;  // Copy on read
   ```

2. **`GetPayloadRef()`** - Zero-copy mutable access (loaned samples only)

   ```csharp
   ref var payload = ref sample.GetPayloadRef();
   payload.Field1 = 42;  // Direct modification in shared memory
   ```

3. **`GetPayloadRefReadOnly()`** - Zero-copy read-only access (any sample)
   ```csharp
   ref readonly var payload = ref sample.GetPayloadRefReadOnly();
   Console.WriteLine(payload.Field1);  // Direct read from shared memory
   ```

**Performance Benefits:**

* ✅ No marshaling overhead
* ✅ No intermediate allocations
* ✅ Direct memory access
* ✅ Ideal for large structs or high-frequency updates

## Event Pattern

```csharp
using Iceoryx2;
using Iceoryx2.Event;

// Create a node
var nodeResult = NodeBuilder.New()
    .Name("event_node")
    .Create();

if (!nodeResult.IsOk)
{
    Console.WriteLine($"Failed to create node: {nodeResult}");
    return;
}

using var node = nodeResult.Unwrap();

// Open or create an event service
var serviceResult = node.ServiceBuilder()
    .Event()
    .Open("MyEventService");

if (!serviceResult.IsOk)
{
    Console.WriteLine($"Failed to open event service: {serviceResult}");
    return;
}

using var service = serviceResult.Unwrap();

// Notifier example (event sender)
var notifierResult = service.CreateNotifier(defaultEventId: new EventId(100));
if (!notifierResult.IsOk)
{
    Console.WriteLine($"Failed to create notifier: {notifierResult}");
    return;
}

using var notifier = notifierResult.Unwrap();

var notifyResult = notifier.Notify(new EventId(5));
if (!notifyResult.IsOk)
{
    Console.WriteLine($"Failed to notify: {notifyResult}");
}

// Listener example (event receiver)
var listenerResult = service.CreateListener();
if (!listenerResult.IsOk)
{
    Console.WriteLine($"Failed to create listener: {listenerResult}");
    return;
}

using var listener = listenerResult.Unwrap();

// Non-blocking wait
var tryWaitResult = listener.TryWait();
if (!tryWaitResult.IsOk)
{
    Console.WriteLine($"Failed to wait: {tryWaitResult}");
    return;
}

var eventId = tryWaitResult.Unwrap();
if (eventId.HasValue)
{
    Console.WriteLine($"Received event: {eventId.Value}");
}

// Timed wait (1 second timeout)
var timedWaitResult = listener.TimedWait(TimeSpan.FromSeconds(1));
if (!timedWaitResult.IsOk)
{
    Console.WriteLine($"Failed to wait: {timedWaitResult}");
    return;
}

var timedEventId = timedWaitResult.Unwrap();
if (timedEventId.HasValue)
{
    Console.WriteLine($"Received event: {timedEventId.Value}");
}
else
{
    Console.WriteLine("Timeout - no event received");
}

// Blocking wait
var blockingWaitResult = listener.BlockingWait();
if (!blockingWaitResult.IsOk)
{
    Console.WriteLine($"Failed to wait: {blockingWaitResult}");
    return;
}

var blockingEventId = blockingWaitResult.Unwrap();
Console.WriteLine($"Received event: {blockingEventId}");
```

## WaitSet Event Multiplexing

The `WaitSet` enables efficient monitoring of multiple event sources
simultaneously without polling. It uses OS-level primitives
(epoll on Linux, kqueue on macOS) to wake only when events arrive.

### Benefits

* **No CPU polling** - Uses OS-level event notification, not busy loops
* **Multiple sources** - Monitor many listeners in a single wait call
* **Signal handling** - Built-in support for graceful shutdown (Ctrl+C)
* **Async integration** - Run WaitSet in background task

### Basic WaitSet Usage

```csharp
using Iceoryx2;

// Create node and event services
var node = NodeBuilder.New().Create().Unwrap();

var service1 = node.ServiceBuilder()
    .Event()
    .Open("events1")
    .Unwrap();

var service2 = node.ServiceBuilder()
    .Event()
    .Open("events2")
    .Unwrap();

// Create listeners
using var listener1 = service1.CreateListener().Unwrap();
using var listener2 = service2.CreateListener().Unwrap();

// Create WaitSet with signal handling
using var waitset = WaitSetBuilder.New()
    .SignalHandling(SignalHandlingMode.TerminationAndInterrupt)
    .Create()
    .Unwrap();

// Attach listeners to WaitSet
using var guard1 = waitset.AttachNotification(listener1).Unwrap();
using var guard2 = waitset.AttachNotification(listener2).Unwrap();

// Event processing callback
CallbackProgression OnEvent(WaitSetAttachmentId attachmentId)
{
    if (attachmentId.HasEventFrom(guard1))
    {
        // CRITICAL: Consume ALL pending events to avoid busy loop
        while (listener1.TryWait().Unwrap() is { } eventId)
        {
            Console.WriteLine($"Service 1 event: {eventId.Value}");
        }
    }
    else if (attachmentId.HasEventFrom(guard2))
    {
        while (listener2.TryWait().Unwrap() is { } eventId)
        {
            Console.WriteLine($"Service 2 event: {eventId.Value}");
        }
    }

    return CallbackProgression.Continue;
}

// Run event loop (blocks until signal received)
waitset.WaitAndProcess(OnEvent);
```

### Critical Pattern: Consume All Events

The WaitSet wakes when events are available. You **must consume ALL pending
events** in your callback:

```csharp
// ❌ WRONG: Only consumes one event - causes busy loop!
if (attachmentId.HasEventFrom(guard))
{
    var eventId = listener.TryWait().Unwrap();
    Console.WriteLine($"Event: {eventId}");
}

// ✅ CORRECT: Consume all pending events
if (attachmentId.HasEventFrom(guard))
{
    while (true)
    {
        var result = listener.TryWait();
        if (!result.IsOk) break;

        var eventIdOpt = result.Unwrap();
        if (!eventIdOpt.HasValue) break;

        Console.WriteLine($"Event: {eventIdOpt.Value}");
    }
}
```

If events aren't fully consumed, the file descriptor remains ready and
the WaitSet immediately wakes again, creating a CPU-burning busy loop.

## Request-Response Pattern (RPC)

The Request-Response API provides a complete client-server RPC implementation
with support for both convenience methods and zero-copy operations.

```csharp
using Iceoryx2;
using Iceoryx2.RequestResponse;
using System.Runtime.InteropServices;

// Define request and response types
[StructLayout(LayoutKind.Sequential)]
public struct AddRequest
{
    public int Value;
}

[StructLayout(LayoutKind.Sequential)]
public struct AddResponse
{
    public int Sum;
}

// Create a node
var nodeResult = NodeBuilder.New()
    .Name("rpc_node")
    .Create();

if (!nodeResult.IsOk)
{
    Console.WriteLine($"Failed to create node: {nodeResult}");
    return;
}

using var node = nodeResult.Unwrap();

// Open or create a request-response service
var serviceResult = node.ServiceBuilder()
    .RequestResponse<AddRequest, AddResponse>()
    .Open("AddService");

if (!serviceResult.IsOk)
{
    Console.WriteLine($"Failed to open service: {serviceResult}");
    return;
}

using var service = serviceResult.Unwrap();

// Client example - send request and wait for response
var clientResult = service.CreateClient();
if (!clientResult.IsOk)
{
    Console.WriteLine($"Failed to create client: {clientResult}");
    return;
}

using var client = clientResult.Unwrap();

// Option 1: SendCopy() - Convenience method that copies data
var pendingResult = client.SendCopy(new AddRequest { Value = 42 });
if (!pendingResult.IsOk)
{
    Console.WriteLine($"Failed to send request: {pendingResult}");
    return;
}

using var pending = pendingResult.Unwrap();

// Wait for response with timeout (non-blocking, timed, or blocking)
var responseResult = pending.TimedReceive(TimeSpan.FromSeconds(2));
if (!responseResult.IsOk)
{
    Console.WriteLine($"Failed to receive response: {responseResult}");
    return;
}

var response = responseResult.Unwrap();
if (response != null)
{
    using (response)
    {
        Console.WriteLine($"Response sum: {response.Payload.Sum}");
    }
}
else
{
    Console.WriteLine("Request timed out");
}

// Option 2: Loan() - Zero-copy method for better performance
var loanResult = client.Loan();
if (loanResult.IsOk)
{
    using var request = loanResult.Unwrap();
    request.Payload = new AddRequest { Value = 42 };

    var sendResult = request.Send();
    if (sendResult.IsOk)
    {
        using var pendingResponse = sendResult.Unwrap();
        // Handle response...
    }
}

// Server example - receive request and send response
var serverResult = service.CreateServer();
if (!serverResult.IsOk)
{
    Console.WriteLine($"Failed to create server: {serverResult}");
    return;
}

using var server = serverResult.Unwrap();

while (true)
{
    var requestResult = server.Receive();
    if (!requestResult.IsOk)
    {
        Console.WriteLine($"Failed to receive request: {requestResult}");
        break;
    }

    var request = requestResult.Unwrap();
    if (request != null)
    {
        using (request)
        {
            int value = request.Payload.Value;

            // Option 1: SendCopyResponse() - Convenience method
            var sendResult = request.SendCopyResponse(new AddResponse { Sum = value + 100 });
            if (!sendResult.IsOk)
            {
                Console.WriteLine($"Failed to send response: {sendResult}");
            }

            // Option 2: LoanResponse() - Zero-copy method
            // var loanResult = request.LoanResponse();
            // if (loanResult.IsOk)
            // {
            //     using var response = loanResult.Unwrap();
            //     response.Payload = new AddResponse { Sum = value + 100 };
            //     response.Send();
            // }
        }
    }

    Thread.Sleep(100); // Small delay between checks
}
```

**Key Features:**

* ✅ Fully verified FFI signatures matching the C API exactly
* ✅ Both convenience methods (`SendCopy`, `SendCopyResponse`) and zero-copy
  methods (`Loan`, `LoanResponse`)
* ✅ Three response waiting modes: non-blocking (`TryReceive`), timed (`TimedReceive`),
  and blocking (`BlockingReceive`)
* ✅ Proper memory management with automatic cleanup
* ✅ Type-safe request/response handling with generic types

## Complex Data Types

The bindings support complex data types using sequential layout:

```csharp
using System.Runtime.InteropServices;
using Iceoryx2;

[StructLayout(LayoutKind.Sequential)]
[Iox2Type("TransmissionData")]  // Optional: specify custom type name
public struct TransmissionData
{
    public int X;
    public int Y;
    public double Value;
}

// Use with publish-subscribe
var service = node.ServiceBuilder()
    .PublishSubscribe<TransmissionData>()
    .Open("ComplexDataService")
    .Unwrap();

using var publisher = service.CreatePublisher().Unwrap();
using var sample = publisher.Loan().Unwrap();

sample.Payload = new TransmissionData
{
    X = 10,
    Y = 20,
    Value = 3.14
};

sample.Send();
```

## Service Configuration (Quality of Service)

iceoryx2 provides extensive configuration options for fine-tuning service
behavior. These settings control memory usage, buffer sizes, and performance
characteristics.

### Publish-Subscribe Service Configuration

```csharp
var service = node.ServiceBuilder()
    .PublishSubscribe<MyData>()
    // Maximum number of subscribers that can connect (default: 8)
    .MaxSubscribers(16)
    // Maximum number of publishers that can connect (default: 2)
    .MaxPublishers(4)
    // Subscriber buffer size - how many samples each subscriber can hold (default: 2)
    .SubscriberMaxBufferSize(10)
    // Maximum samples a subscriber can borrow at once (default: 2)
    .SubscriberMaxBorrowedSamples(3)
    // History size for late-joining subscribers (default: 0)
    .HistorySize(5)
    // Enable safe overflow - oldest sample replaced when buffer full (default: true)
    .EnableSafeOverflow(true)
    .Open("MyService")
    .Unwrap();
```

### Publisher Configuration

```csharp
var publisher = service.CreatePublisherBuilder()
    // Maximum samples the publisher can loan at once (default: 2)
    .MaxLoanedSamples(4)
    .Create()
    .Unwrap();
```

### Subscriber Configuration

```csharp
var subscriber = service.CreateSubscriberBuilder()
    // Override the buffer size for this specific subscriber
    .BufferSize(20)
    .Create()
    .Unwrap();
```

### Configuration Best Practices

| Setting | Low Memory | High Throughput | Reliability |
|---------|------------|-----------------|-------------|
| `SubscriberMaxBufferSize` | 1-2 | 10+ | 5+ |
| `HistorySize` | 0 | 0 | 5+ |
| `MaxLoanedSamples` | 1 | 4+ | 2 |
| `EnableSafeOverflow` | true | true | false |

**Key Considerations:**

* **Memory Usage**: Each subscriber buffer consumes
  `buffer_size × payload_size` bytes
* **History Size**: Useful for late-joining subscribers, but increases memory
* **Safe Overflow**: When enabled, slow subscribers won't block fast publishers
* **Loaned Samples**: Higher values allow more concurrent writes but use more
  memory

## Async/Await Support

The C# bindings provide full async/await support for all blocking operations,
enabling modern asynchronous programming patterns with proper cancellation support.

### Benefits

* **Non-blocking** - Operations yield to the thread pool instead of blocking threads
* **Composable** - Use `Task.WhenAll()`, `Task.WhenAny()` for concurrent operations
* **Cancellable** - All async methods accept `CancellationToken` for
  cooperative cancellation
* **Efficient** - Better thread pool utilization compared to polling with `Thread.Sleep()`

### Async Methods

All classes with blocking operations provide async equivalents:

#### PendingResponse (Request-Response)

```csharp
// Synchronous methods (block the calling thread)
Result<Response<T>?, Iox2Error> TryReceive()
Result<Response<T>?, Iox2Error> TimedReceive(TimeSpan timeout)
Result<Response<T>, Iox2Error> BlockingReceive()

// Asynchronous methods (yield to thread pool)
Task<Result<Response<T>?, Iox2Error>> ReceiveAsync(TimeSpan timeout, CancellationToken ct = default)
Task<Result<Response<T>, Iox2Error>> ReceiveAsync(CancellationToken ct = default)
```

#### Listener (Events)

```csharp
// Synchronous methods (block the calling thread)
Result<EventId?, Iox2Error> TryWait()
Result<EventId?, Iox2Error> TimedWait(TimeSpan timeout)
Result<EventId, Iox2Error> BlockingWait()

// Asynchronous methods (offload to background thread)
Task<Result<EventId?, Iox2Error>> WaitAsync(TimeSpan timeout, CancellationToken ct = default)
Task<Result<EventId, Iox2Error>> WaitAsync(CancellationToken ct = default)
```

#### Subscriber (Publish-Subscribe)

```csharp
// Synchronous method (non-blocking poll)
Result<Sample<T>?, Iox2Error> Receive<T>()

// Asynchronous methods (poll with yielding to thread pool)
Task<Result<Sample<T>?, Iox2Error>> ReceiveAsync<T>(TimeSpan timeout, CancellationToken ct = default)
Task<Result<Sample<T>, Iox2Error>> ReceiveAsync<T>(CancellationToken ct = default)
```

**Note:** Subscriber async methods use polling (every 10ms) since the native API
doesn't provide blocking receive. However, they yield to the thread pool efficiently.

### Example: Async Request-Response Client

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Iceoryx2;
using Iceoryx2.RequestResponse;

public async Task RunClientAsync(CancellationToken cancellationToken = default)
{
    // Create node and service (same as sync version)
    var node = NodeBuilder.New()
        .Name("async_client")
        .Create()
        .Unwrap();

    using var service = node.ServiceBuilder()
        .RequestResponse<ulong, MyResponse>()
        .Open("MyService")
        .Unwrap();

    using var client = service.CreateClient().Unwrap();

    // Send request
    var sendResult = client.SendCopy(42ul);
    using var pendingResponse = sendResult.Unwrap();

    // Wait for response asynchronously with timeout
    var responseResult = await pendingResponse.ReceiveAsync(
        TimeSpan.FromSeconds(2),
        cancellationToken);

    if (responseResult.IsOk)
    {
        var response = responseResult.Unwrap();
        if (response != null)
        {
            using (response)
            {
                Console.WriteLine($"Received: {response.Payload}");
            }
        }
        else
        {
            Console.WriteLine("Request timed out");
        }
    }
}
```

### Example: Async Event Listener

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Iceoryx2;

public async Task RunListenerAsync(CancellationToken cancellationToken = default)
{
    var node = NodeBuilder.New()
        .Name("async_listener")
        .Create()
        .Unwrap();

    using var service = node.ServiceBuilder()
        .Event()
        .Open("MyEvents")
        .Unwrap();

    using var listener = service.CreateListener().Unwrap();

    // Wait for events asynchronously
    while (!cancellationToken.IsCancellationRequested)
    {
        var result = await listener.WaitAsync(
            TimeSpan.FromSeconds(5),
            cancellationToken);

        if (result.IsOk)
        {
            var eventId = result.Unwrap();
            if (eventId.HasValue)
            {
                Console.WriteLine($"Received event: {eventId.Value}");
            }
            else
            {
                Console.WriteLine("Timeout - no event");
            }
        }
    }
}
```

### Best Practices

**1. Use async methods in async contexts:**

```csharp
// ✅ GOOD: Async all the way
public async Task ProcessDataAsync()
{
    var response = await pendingResponse.ReceiveAsync(TimeSpan.FromSeconds(1));
    // ... process response
}

// ❌ BAD: Blocking in async method
public async Task ProcessDataAsync()
{
    var response = pendingResponse.TimedReceive(TimeSpan.FromSeconds(1)); // Blocks!
}
```

**2. Always pass CancellationToken:**

```csharp
// ✅ GOOD: Cancellable operation
public async Task WorkAsync(CancellationToken ct)
{
    var response = await pendingResponse.ReceiveAsync(TimeSpan.FromSeconds(10), ct);
}

// ⚠️ OK but less flexible: No cancellation
public async Task WorkAsync()
{
    var response = await pendingResponse.ReceiveAsync(TimeSpan.FromSeconds(10));
}
```

**3. Use ConfigureAwait(false) in libraries:**

```csharp
// In library code, avoid capturing SynchronizationContext
var response = await pendingResponse
    .ReceiveAsync(timeout, ct)
    .ConfigureAwait(false);
```

**4. Combine with Task composition:**

```csharp
// Wait for multiple responses concurrently
var tasks = new[]
{
    pending1.ReceiveAsync(timeout, ct),
    pending2.ReceiveAsync(timeout, ct),
    pending3.ReceiveAsync(timeout, ct)
};

var responses = await Task.WhenAll(tasks);

// Or race for the first response
var firstResponse = await Task.WhenAny(tasks);
```

## Naming Convention

The C# bindings follow .NET naming conventions:

* **Classes** use PascalCase (e.g., `Node`, `ServiceBuilder`, `EventService`)
* **Methods** use PascalCase (e.g., `Create()`, `OpenOrCreate()`, `Notify()`)
* **Properties** use PascalCase (e.g., `Name`, `Id`, `Payload`)
* **Internal/Native types** use the original C naming with `iox2_` prefix
* **Result pattern** uses `IsOk` property and `Unwrap()` method for error handling

## API Patterns

### Result Type

All fallible operations return a `Result<T, Iox2Error>` type:

```csharp
var result = node.ServiceBuilder().Event().Open("MyService");

// Check for success
if (!result.IsOk)
{
    Console.WriteLine($"Error: {result}");
    return;
}

// Unwrap the value (only call after checking IsOk)
using var service = result.Unwrap();
```

### Builder Pattern

The bindings use a fluent builder pattern for configuration:

```csharp
var node = NodeBuilder.New()
    .Name("my_node")
    .Create()
    .Unwrap();

var service = node.ServiceBuilder()
    .PublishSubscribe<int>()
    .Open("MyService")
    .Unwrap();

var publisher = service.CreatePublisher()
    .Unwrap();
```

## Service Discovery

The Service Discovery API allows you to dynamically discover running services
and inspect their configurations. This is useful for monitoring, debugging,
and building dynamic service-aware applications.

### Listing Available Services

```csharp
using Iceoryx2;

// Create a node (required to access service discovery)
using var node = NodeBuilder.New()
    .Name("discovery_node")
    .Create()
    .Unwrap();

// List all running services
var services = node.List().Unwrap();

Console.WriteLine($"Found {services.Count} service(s):");

foreach (var service in services)
{
    Console.WriteLine($"  Service: {service.Name}");
    Console.WriteLine($"  ID:      {service.Id}");
    Console.WriteLine($"  Pattern: {service.MessagingPattern}");
}
```

### Inspecting Service Configuration

Each service provides detailed configuration based on its messaging pattern:

```csharp
foreach (var service in services)
{
    switch (service.MessagingPattern)
    {
        case MessagingPattern.PublishSubscribe:
            var pubSubConfig = service.PublishSubscribeConfig;
            Console.WriteLine($"  Max Publishers:   {pubSubConfig.MaxPublishers}");
            Console.WriteLine($"  Max Subscribers:  {pubSubConfig.MaxSubscribers}");
            Console.WriteLine($"  History Size:     {pubSubConfig.HistorySize}");
            Console.WriteLine($"  Buffer Size:      {pubSubConfig.SubscriberMaxBufferSize}");
            break;

        case MessagingPattern.Event:
            var eventConfig = service.EventConfig;
            Console.WriteLine($"  Max Notifiers:    {eventConfig.MaxNotifiers}");
            Console.WriteLine($"  Max Listeners:    {eventConfig.MaxListeners}");
            Console.WriteLine($"  Max Event ID:     {eventConfig.EventIdMaxValue}");
            break;

        case MessagingPattern.RequestResponse:
            var rpcConfig = service.RequestResponseConfig;
            Console.WriteLine($"  Max Clients:      {rpcConfig.MaxClients}");
            Console.WriteLine($"  Max Servers:      {rpcConfig.MaxServers}");
            break;
    }
}
```

### Use Cases

* **Monitoring dashboards** - Display real-time service status
* **Service health checks** - Verify expected services are running
* **Dynamic routing** - Route messages based on available services
* **Debugging** - Inspect service configurations to diagnose issues
* **Service mesh integration** - Build service registries and load balancers

## Memory Management

The C# bindings implement proper memory management with multiple layers of safety:

* **All native resources implement `IDisposable`** - ensures cleanup even if
  exceptions occur
* **Use `using` statements** to ensure proper cleanup of resources
* **`SafeHandle` types** protect against resource leaks and race conditions
* **Automatic finalization** for cleanup if `Dispose()` is not called
  (though explicit disposal is recommended)
* **No manual memory management required** - the bindings handle all FFI marshalling

### Best Practices

```csharp
// ✅ GOOD: Using statement ensures disposal
using var node = NodeBuilder.New().Create().Unwrap();
using var service = node.ServiceBuilder().Event().Open("MyService").Unwrap();
using var notifier = service.CreateNotifier().Unwrap();

// ✅ GOOD: Explicit disposal in try-finally
var node = NodeBuilder.New().Create().Unwrap();
try
{
    // Use node...
}
finally
{
    node.Dispose();
}

// ❌ BAD: No disposal - relies on finalizer (slower, not deterministic)
var node = NodeBuilder.New().Create().Unwrap();
// ... use node without disposing
```

## Features

### Supported Communication Patterns

* ✅ **Publish-Subscribe** - One-to-many data distribution with zero-copy
* ✅ **Event** - Lightweight notification system with custom event IDs
* ✅ **Request-Response** - Client-server RPC with async response handling
* 🚧 **Pipeline** - Coming soon

### Supported Platforms

* ✅ **macOS** (tested on Apple Silicon and Intel)
* ✅ **Linux** (x86_64, ARM64)
* ✅ **Windows** (x86_64)

### Type System

* ✅ **Primitive types** - int, uint, long, ulong, float, double, bool
* ✅ **Complex types** - Structs with `[StructLayout(LayoutKind.Sequential)]`
* ✅ **Custom type names** - Use `[Iox2Type("name")]` attribute
* ⚠️ **Zero-copy** - Requires sequential layout and unmanaged types

## Troubleshooting

### Native Library Not Found

If you get a `DllNotFoundException`, ensure:

1. The native library is built: `cargo build --release --package iceoryx2-ffi-c`
2. The library is in one of these locations:
   * Same directory as your executable
   * System library path (`/usr/lib`, `/usr/local/lib`, etc.)
   * Path specified in `LD_LIBRARY_PATH` (Linux), `DYLD_LIBRARY_PATH` (macOS),
     or `PATH` (Windows)

### Type Name Mismatches

If services can't connect, verify type names match:

```csharp
// Use Iox2Type attribute to ensure consistent naming
[Iox2Type("MyData")]
public struct MyData { ... }
```

For complex types, the bindings automatically generate length-prefixed names
(e.g., `16TransmissionData` for a 16-character struct name). Primitive types use
Rust naming (`i32`, `u64`, etc.).

### Memory Errors or Crashes

* Ensure all resources use `using` statements or are properly disposed
* Don't access samples after calling `Send()` or `Dispose()`
* Use `Result<T, E>` pattern - always check `IsOk` before calling `Unwrap()`

## Examples

The repository includes several complete examples:

### 1. PublishSubscribe

**Location:** `examples/PublishSubscribe/`

Demonstrates basic pub/sub pattern with primitive types:

* Publisher sends incrementing counter values
* Subscriber receives and displays values
* Shows proper resource management with `using` statements

### 2. Event

**Location:** `examples/Event/`

Demonstrates event-based communication:

* Notifier sends events with custom event IDs (0-11)
* Listener receives events with timeout support
* Shows three wait modes: non-blocking, timed, and blocking

### 3. ComplexDataTypes

**Location:** `examples/ComplexDataTypes/`

Demonstrates zero-copy sharing of complex structs:

* Defines custom `TransmissionData` struct
* Shows struct layout and type naming
* Demonstrates cross-process struct sharing

### 4. RequestResponse

**Location:** `examples/RequestResponse/`

Demonstrates client-server RPC pattern with fully verified C API compatibility:

* Client sends `AddRequest` messages with integer values
* Server maintains a running sum and responds with `AddResponse`
* Shows async response handling with three wait modes (non-blocking, timed, blocking)
* Demonstrates both `SendCopy()` convenience method and `Loan()`/`LoanResponse()`
  for zero-copy
* FFI signatures verified to exactly match the C API for reliable operation

### 5. AsyncPubSub

**Location:** `examples/AsyncPubSub/`

Demonstrates modern async/await patterns for publish-subscribe:

* Async publisher using `await Task.Delay()` instead of blocking
* Async subscriber with timeout using `ReceiveAsync()`
* Async subscriber blocking until data arrives
* Multiple concurrent subscribers processing data in parallel
* Proper cancellation support with `CancellationToken`
* Shows best practices for async IPC in modern C# applications

**Run with:**

```bash
# Terminal 1 - Async publisher
cd examples/AsyncPubSub
dotnet run --framework net9.0 publisher

# Terminal 2 - Async subscriber with timeout
dotnet run --framework net9.0 subscriber

# Or try other modes: blocking, multi
dotnet run --framework net9.0 blocking
dotnet run --framework net9.0 multi
```

### 6. WaitSetMultiplexing

**Location:** `examples/WaitSetMultiplexing/`

Demonstrates efficient event multiplexing using WaitSet:

* Monitor multiple event services in a single wait call
* Uses OS-level primitives (epoll/kqueue) for efficient waiting
* Signal handling for graceful shutdown (Ctrl+C)
* Shows proper event consumption pattern to avoid busy loops

**Run with:**

```bash
# Terminal 1 - Wait on multiple services
cd examples/WaitSetMultiplexing
dotnet run --framework net9.0 wait service1 service2

# Terminal 2 - Send events
dotnet run --framework net9.0 notify 42 service1
dotnet run --framework net9.0 notify 100 service2
```

### 7. ServiceDiscovery

**Location:** `examples/ServiceDiscovery/`

Demonstrates dynamic service discovery:

* List all running services in the system
* Inspect service configurations (publishers, subscribers, buffer sizes)
* Identify messaging patterns (Pub/Sub, Event, Request-Response)
* Useful for monitoring and debugging

**Run with:**

```bash
# First, start some services in another terminal (e.g., PublishSubscribe example)
# Then run the discovery example:
cd examples/ServiceDiscovery
dotnet run --framework net9.0
```
