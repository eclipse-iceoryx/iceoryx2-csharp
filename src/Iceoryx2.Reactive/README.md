# iceoryx2.Reactive

Reactive Extensions (Rx) support for iceoryx2 - provides `IObservable<T>` pattern for declarative, composable pub/sub communication.

## Overview

`Iceoryx2.Reactive` transforms iceoryx2's imperative polling-based subscriber into a declarative, Rx-style data stream. This enables powerful LINQ-style operators and clean async/await patterns.

### Before: Imperative Polling

```csharp
while (true)
{
    var result = subscriber.Receive<MyData>();
    if (result.IsOk)
    {
        var sample = result.Unwrap();
        if (sample.HasValue)
        {
            using var s = sample.Value;
            // Process data
            Console.WriteLine($"Received: {s.Payload}");
        }
    }
    Thread.Sleep(10);
}
```

### After: Declarative Rx Stream

```csharp
using var subscription = subscriber.AsObservable<MyData>()
    .Where(data => data.IsValid)
    .Subscribe(data => Console.WriteLine($"Received valid data: {data}"));

Console.ReadKey(); // Keep the app alive
```

## Features

* ✅ **IObservable<T>** - Full Rx integration with System.Reactive
* ✅ **LINQ Operators** - Use Where, Select, Buffer, Throttle, etc.
* ✅ **Async Streams** - IAsyncEnumerable<T> support for `await foreach`
* ✅ **Composable** - Chain and combine multiple streams
* ✅ **Cancellation** - Full CancellationToken support across all async operations
* ✅ **Resource Management** - RAII disposal patterns
* ✅ **Two Modes**:
    * **Polling-based** - Simple, works everywhere, configurable polling interval
    * **Event-driven (WaitSet)** - Truly async using OS primitives (epoll/kqueue), low latency, low CPU
* ✅ **Comprehensive Async API** - Every blocking operation has an async counterpart:
    * `Subscriber.ReceiveAsync<T>()` - Async receive with optional timeout
    * `Listener.WaitAsync()` - Async event waiting with optional timeout
    * `PendingResponse.ReceiveAsync()` - Async response receiving with optional timeout

## Two Approaches: Polling vs. Event-Driven

### 1. Subscriber - Polling-Based (Data Streams)

Subscribers provide data samples and use polling since the native API doesn't have blocking receive:

```csharp
// Polling every 10ms (configurable)
using var subscription = subscriber.AsObservable<SensorData>(
    pollingInterval: TimeSpan.FromMilliseconds(10))
    .Where(data => data.Temperature > 28.0)
    .Subscribe(data => Console.WriteLine($"Hot: {data.Temperature}°C"));
```

**Characteristics:**

* ⚠️ Polls with configurable interval (default: 10ms)
* ⚠️ Minimum latency = polling interval
* ⚠️ Periodic CPU wake-ups
* ✅ Simple to use
* ✅ Good for data stream processing

### 2. Listener - Event-Driven with WaitSet (Event Notifications)

Listeners provide lightweight event notifications and use WaitSet for truly async, event-driven operation:

```csharp
// Event-driven using WaitSet - no polling!
using var subscription = listener.AsObservable(
    deadline: TimeSpan.FromSeconds(1))  // Optional deadline
    .Subscribe(eventId => Console.WriteLine($"Event: {eventId.Value}"));
```

**Characteristics:**

* ✅ **Zero polling** - OS wakes thread only on events (epoll/kqueue)
* ✅ **Low latency** - immediate wake-up when events arrive
* ✅ **Low CPU** - thread sleeps until events
* ✅ **Deadline support** - optional timeout for event arrival
* ✅ **Production-ready** for event-driven architectures

### Architecture: Pub/Sub vs. Event System

**Publish-Subscribe (Subscriber):**

* Used for high-throughput data streams
* Transfers actual payload data (zero-copy)
* Native API is polling-based → Reactive Extensions use polling
* Use for: sensor data, telemetry, video frames, large datasets

**Event System (Listener/Notifier):**

* Used for lightweight event notifications
* Transfers only event IDs (no payload)
* Native API supports WaitSet → Reactive Extensions are truly event-driven
* Use for: state changes, triggers, control signals, coordination

## Modern Async/Await Integration

The iceoryx2 C# wrapper is designed to be truly async-first. **Every potentially blocking or long-running operation has an async counterpart that accepts a `CancellationToken`**, allowing seamless integration into modern asynchronous applications without ever blocking threads.

### Core Async APIs

#### Subscriber - Async Receive

```csharp
// Wait indefinitely for a sample
var result = await subscriber.ReceiveAsync<MyData>(cancellationToken);

// Wait with timeout
var result = await subscriber.ReceiveAsync<MyData>(TimeSpan.FromSeconds(5), cancellationToken);
```

#### Listener - Async Event Waiting

```csharp
// Wait indefinitely for an event
var result = await listener.WaitAsync(cancellationToken);

// Wait with timeout
var result = await listener.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
```

#### PendingResponse - Async Request/Response

```csharp
// Wait indefinitely for a response
var result = await pendingResponse.ReceiveAsync(cancellationToken);

// Wait with timeout
var result = await pendingResponse.ReceiveAsync(TimeSpan.FromSeconds(5), cancellationToken);
```

### Reactive Extensions - Declarative Streams

**For data streams (Subscriber - polling-based):**

```csharp
using var subscription = subscriber.AsObservable<MyData>(cancellationToken: cts.Token)
    .Where(data => data.IsValid)
    .Subscribe(data => Console.WriteLine($"Valid: {data}"));

// Async streams
await foreach (var data in subscriber.AsAsyncEnumerable<MyData>(cancellationToken))
{
    Console.WriteLine($"Received: {data}");
}
```

**For event notifications (Listener - truly event-driven with WaitSet):**

```csharp
using var subscription = listener.AsObservable(
    deadline: TimeSpan.FromSeconds(1),
    cancellationToken: cts.Token)
    .Subscribe(eventId => Console.WriteLine($"Event: {eventId.Value}"));

// Async streams
await foreach (var eventId in listener.AsAsyncEnumerable(
    deadline: TimeSpan.FromSeconds(1),
    cancellationToken))
{
    Console.WriteLine($"Event: {eventId.Value}");
}
```

## Installation

Add package reference to your project:

```xml
<ItemGroup>
  <ProjectReference Include="../path/to/Iceoryx2.Reactive/Iceoryx2.Reactive.csproj" />
</ItemGroup>
```

Or via NuGet (when published):

```bash
dotnet add package Iceoryx2.Reactive
```

## Usage

### Basic Observable (Polling-Based)

```csharp
using Iceoryx2;
using Iceoryx2.Reactive;
using System.Reactive.Linq;

var node = NodeBuilder.New().Create().Expect("Failed to create node");
var service = node.ServiceBuilder()
    .PublishSubscribe<MyData>()
    .Open("my_service")
    .Expect("Failed to open service");

var subscriber = service.CreateSubscriber()
    .Expect("Failed to create subscriber");

// Polling-based observable (simple)
using var subscription = subscriber.AsObservable<MyData>()
    .Subscribe(data => Console.WriteLine($"Received: {data}"));

Console.ReadKey();
```

### Event-Driven Observable with WaitSet (Recommended)

```csharp
using Iceoryx2;
using Iceoryx2.Reactive;
using System.Reactive.Linq;

var node = NodeBuilder.New().Create().Expect("Failed to create node");
var service = node.ServiceBuilder()
    .PublishSubscribe<MyData>()
    .Open("my_service")
    .Expect("Failed to open service");

var subscriber = service.CreateSubscriber()
    .Expect("Failed to create subscriber");

// Event-driven observable using WaitSet (truly async!)
using var subscription = subscriber.AsObservableWithWaitSet<MyData>(
    deadline: TimeSpan.FromSeconds(1))  // Optional deadline
    .Subscribe(data => Console.WriteLine($"Received: {data}"));

Console.ReadKey();
```

### LINQ Operators (Works with Both Modes)

```csharp
// Polling-based with LINQ
using var subscription = subscriber.AsObservable<SensorData>()
    .Where(data => data.Temperature > 25.0)
    .Select(data => new { data.Temperature, IsCritical = data.Temperature > 50.0 })
    .Subscribe(result => 
        Console.WriteLine($"Temp: {result.Temperature}°C, Critical: {result.IsCritical}"));

// Event-driven with WaitSet + LINQ (recommended for production)
using var subscription = subscriber.AsObservableWithWaitSet<SensorData>(
    deadline: TimeSpan.FromSeconds(2))
    .Where(data => data.Temperature > 25.0)
    .Select(data => new { data.Temperature, IsCritical = data.Temperature > 50.0 })
    .Subscribe(result => 
        Console.WriteLine($"Temp: {result.Temperature}°C, Critical: {result.IsCritical}"));
```

### Buffering and Throttling

```csharp
// Process in batches every 100ms (event-driven)
using var subscription = subscriber.AsObservableWithWaitSet<LogEntry>()
    .Buffer(TimeSpan.FromMilliseconds(100))
    .Subscribe(batch => 
        Console.WriteLine($"Received batch of {batch.Count} log entries"));

// Throttle to max 10 items/second
using var subscription2 = subscriber.AsObservable<Event>()
    .Sample(TimeSpan.FromMilliseconds(100))
    .Subscribe(evt => ProcessEvent(evt));
```

### Multiple Subscribers (Merge)

```csharp
var obs1 = subscriber1.AsObservable<MyData>();
var obs2 = subscriber2.AsObservable<MyData>();

// Merge multiple streams
using var subscription = obs1.Merge(obs2)
    .Subscribe(data => Console.WriteLine($"From either stream: {data}"));
```

### Async Enumerable (await foreach)

```csharp
await foreach (var data in subscriber.AsAsyncEnumerable<MyData>(cancellationToken))
{
    Console.WriteLine($"Received: {data}");
    
    if (data.ShouldStop)
        break;
}
```

### Custom Polling Interval

```csharp
// Poll every 1ms for low-latency scenarios
using var subscription = subscriber.AsObservable<MyData>(
    pollingInterval: TimeSpan.FromMilliseconds(1))
    .Subscribe(data => ProcessHighFrequency(data));

// Poll every 100ms for low CPU usage
using var subscription2 = subscriber.AsObservable<MyData>(
    pollingInterval: TimeSpan.FromMilliseconds(100))
    .Subscribe(data => ProcessLowFrequency(data));
```

### With Cancellation

```csharp
using var cts = new CancellationTokenSource();

using var subscription = subscriber.AsObservable<MyData>(
    cancellationToken: cts.Token)
    .Subscribe(
        data => Console.WriteLine($"Received: {data}"),
        error => Console.WriteLine($"Error: {error}"),
        () => Console.WriteLine("Stream completed"));

// Later...
cts.Cancel(); // Stops the observable stream gracefully
```

## API Reference

### SubscriberExtensions.AsObservable<T>()

Converts a Subscriber into an `IObservable<T>` stream.

**Parameters:**

* `pollingInterval` (optional) - Polling interval (default: 10ms)
* `cancellationToken` (optional) - Token to cancel the stream

**Returns:** `IObservable<T>`

### SubscriberExtensions.AsAsyncEnumerable<T>()

Converts a Subscriber into an `IAsyncEnumerable<T>` stream.

**Parameters:**

* `pollingInterval` (optional) - Polling interval (default: 10ms)
* `cancellationToken` (optional) - Token to cancel the stream

**Returns:** `IAsyncEnumerable<T>`

## Performance Considerations

### Polling vs. Event-Driven

⚠️ **Important**: The Reactive Extensions are **polling-based**, not truly event-driven. The `AsObservable<T>()` and `AsAsyncEnumerable<T>()` methods poll the subscriber with `Task.Delay()` between checks.

**For truly event-driven, low-latency operations**, use the **WaitSet** API instead, which blocks on platform-specific OS primitives (epoll on Linux, kqueue on macOS) and wakes only when events occur - no polling overhead.

**When to use Reactive Extensions:**

* ✅ Declarative data processing pipelines
* ✅ LINQ-style transformations and filtering
* ✅ Simple scripts and prototypes
* ⚠️ Not ideal for ultra-low-latency production systems

**When to use WaitSet:**

* ✅ Production event-driven applications
* ✅ Multiple event sources with single wait
* ✅ Ultra-low latency requirements
* ✅ Minimal CPU usage

### Polling Interval Trade-offs

* **Lower interval (1-5ms)**
    * ✅ Lower latency
    * ❌ Higher CPU usage
    * Use for: Real-time systems, high-frequency data

* **Default interval (10ms)**
    * ✅ Balanced latency/CPU
    * Use for: Most applications

* **Higher interval (50-100ms)**
    * ✅ Lower CPU usage
    * ❌ Higher latency
    * Use for: Background processing, non-critical data

### CPU Usage

The observable continuously polls the subscriber. Consider:

* Adjusting `pollingInterval` based on your latency requirements
* Using operators like `Throttle()` or `Sample()` to reduce downstream processing
* Disposing subscriptions when not needed

## Examples

See the [examples directory](../../examples/) for complete working examples:

* `ObservableWaitSet` - Observable pattern with event multiplexing
* `WaitSetMultiplexing` - Async/await event handling

## Building

```bash
dotnet build
```

## Testing

```bash
dotnet test
```

## License

Dual-licensed under Apache-2.0 OR MIT
