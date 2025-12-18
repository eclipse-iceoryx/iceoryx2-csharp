# Observable WaitSet Example

This example demonstrates how to use **Rx.NET (Reactive Extensions)** with the
WaitSet API to create powerful reactive event streams. The example shows how to
transform low-level WaitSet events into composable, functional reactive streams.

> **Note**: This example requires the iceoryx2 C library to be built and
> available. See the main iceoryx2 documentation for build instructions.

## Overview

The example provides:

1. **WaitSet to Observable Adapter**: Extension method that converts WaitSet
   events into an `IObservable<EventNotification>` stream
2. **Multiple Reactive Patterns**: Demonstrates filtering, grouping, throttling,
   combining, and async processing
3. **Async Notifier**: Sends events to services for testing

## Key Concepts

### Reactive Extensions (Rx.NET)

Rx.NET provides a powerful functional reactive programming model for composing
asynchronous and event-based programs using observable sequences.

**Benefits:**

* **Declarative**: Express complex event processing logic declaratively
* **Composable**: Chain operators to build sophisticated processing pipelines
* **Time-based**: Built-in support for throttling, buffering, windowing
* **Async-friendly**: Seamless integration with async/await

### WaitSet Observable Extension

The example includes a `ToObservable()` extension method that:

* Wraps WaitSet event processing in an Observable
* Runs WaitSet in background Task
* Properly handles cancellation and disposal
* Emits `EventNotification` records with metadata (service name, event ID, timestamp)

## Building

```bash
dotnet build
```

## Running

### Terminal 1: Start the Observer

Monitor multiple services with reactive streams:

```bash
dotnet run --framework net9.0 -- observe service_a service_b
```

Or with .NET 8.0:

```bash
dotnet run --framework net8.0 -- observe service_a service_b
```

Output:

```text
Creating Observable for services: 'service_a', 'service_b'

=== Simple Event Stream ===

=== Filtered Stream (specific service) ===

=== Event Counting (every 5 seconds) ===

=== Throttled Stream (1/sec max per service) ===

=== Combined Stream (zip multiple services) ===

=== Async Processing ===

✓ All Observable subscriptions active. Press Ctrl+C to stop...
```

### Terminal 2: Send Events to Service A

```bash
dotnet run --framework net9.0 -- notify 123 service_a
```

### Terminal 3: Send Events to Service B

```bash
dotnet run --framework net9.0 -- notify 456 service_b
```

## Reactive Patterns Demonstrated

### 1. Simple Subscription

```csharp
eventStream.Subscribe(
    onNext: evt => Console.WriteLine($"Event: {evt.ServiceName} → {evt.EventId.Value}"),
    onError: ex => Console.WriteLine($"Error: {ex.Message}"),
    onCompleted: () => Console.WriteLine("Completed")
);
```

**Output:**

```text
[10:23:45.123] Service 'service_a' → Event ID: 123
[10:23:46.456] Service 'service_b' → Event ID: 456
```

### 2. Filtering

Filter events by service name:

```csharp
eventStream
    .Where(evt => evt.ServiceName == "service_a")
    .Subscribe(evt => Console.WriteLine($"Filtered: {evt.EventId.Value}"));
```

**Output:**

```text
Filtered: service_a → 123
```

### 3. Grouping and Buffering

Count events per service over time windows:

```csharp
eventStream
    .GroupBy(evt => evt.ServiceName)
    .SelectMany(group =>
        group.Buffer(TimeSpan.FromSeconds(5))
             .Where(buffer => buffer.Count > 0)
             .Select(buffer => new { Service = group.Key, Count = buffer.Count })
    )
    .Subscribe(stats => Console.WriteLine($"{stats.Service}: {stats.Count} events"));
```

**Output:**

```text
Stats: 'service_a' received 5 events in last 5s
Stats: 'service_b' received 3 events in last 5s
```

### 4. Throttling

Limit event processing rate (max 1 per second per service):

```csharp
eventStream
    .GroupBy(evt => evt.ServiceName)
    .SelectMany(group => group.Throttle(TimeSpan.FromSeconds(1)))
    .Subscribe(evt => Console.WriteLine($"Throttled: {evt.EventId.Value}"));
```

**Output:**

```text
Throttled: service_a → 123
[... 1 second passes ...]
Throttled: service_a → 123
```

### 5. Combining Streams (Zip)

Pair events from two services:

```csharp
var service1Stream = eventStream.Where(e => e.ServiceName == "service_a");
var service2Stream = eventStream.Where(e => e.ServiceName == "service_b");

service1Stream
    .Zip(service2Stream, (e1, e2) => 
        $"Pair: [{e1.ServiceName}:{e1.EventId.Value}] + [{e2.ServiceName}:{e2.EventId.Value}]")
    .Subscribe(msg => Console.WriteLine(msg));
```

**Output:**

```text
Pair: [service_a:123] + [service_b:456]
```

### 6. Async Processing

Process events asynchronously:

```csharp
eventStream
    .SelectMany(async evt =>
    {
        await SomeAsyncOperation(evt);
        return $"Processed: {evt.EventId.Value}";
    })
    .Subscribe(msg => Console.WriteLine(msg));
```

## Advanced Patterns

### Time-Based Windowing

```csharp
eventStream
    .Window(TimeSpan.FromSeconds(10))
    .SelectMany(window => window.Count())
    .Subscribe(count => Console.WriteLine($"Events in last 10s: {count}"));
```

### Debouncing

Wait for quiet period before processing:

```csharp
eventStream
    .Debounce(TimeSpan.FromMilliseconds(500))
    .Subscribe(evt => Console.WriteLine($"Debounced: {evt.EventId.Value}"));
```

### Distinct Until Changed

Only emit when event ID changes:

```csharp
eventStream
    .DistinctUntilChanged(evt => evt.EventId)
    .Subscribe(evt => Console.WriteLine($"New ID: {evt.EventId.Value}"));
```

### Scan (Accumulate)

Accumulate state across events:

```csharp
eventStream
    .Scan(0, (count, evt) => count + 1)
    .Subscribe(total => Console.WriteLine($"Total events: {total}"));
```

## Architecture

```text
┌─────────────────────────────────────────────┐
│         Observer Process                    │
│  ┌───────────────────────────────────────┐  │
│  │    IObservable<EventNotification>     │  │
│  │         (Reactive Stream)             │  │
│  │  ┌─────────────────────────────────┐  │  │
│  │  │  Operators:                     │  │  │
│  │  │  • Where (filter)               │  │  │
│  │  │  • GroupBy (group by service)   │  │  │
│  │  │  • Buffer (time windows)        │  │  │
│  │  │  • Throttle (rate limiting)     │  │  │
│  │  │  • Zip (combine streams)        │  │  │
│  │  │  • SelectMany (async map)       │  │  │
│  │  └─────────────────────────────────┘  │  │
│  │              ↑                         │  │
│  │  ┌───────────┴─────────────────────┐  │  │
│  │  │  WaitSet (OS event multiplex)   │  │  │
│  │  │  ┌──────────┐  ┌──────────┐     │  │  │
│  │  │  │Listener A│  │Listener B│     │  │  │
│  │  │  └────┬─────┘  └────┬─────┘     │  │  │
│  │  └───────┼─────────────┼───────────┘  │  │
│  └──────────┼─────────────┼──────────────┘  │
└─────────────┼─────────────┼─────────────────┘
              │             │
     Events   │             │  Events
              │             │
   ┌──────────▼───────┐  ┌──▼──────────┐
   │   Notifier       │  │  Notifier   │
   │  (service_a)     │  │ (service_b) │
   └──────────────────┘  └─────────────┘
```

## Benefits

1. **Functional Composition**: Chain operators to build complex logic declaratively
2. **Time-Based Operations**: Built-in support for buffering, throttling, debouncing
3. **Async Integration**: Seamless async/await with SelectMany
4. **Error Handling**: Centralized error handling with OnError
5. **Backpressure**: Control event processing rate with throttling/sampling
6. **Testing**: Easy to test with Rx testing utilities
7. **Cancellation**: Proper cancellation token support

## Dependencies

* **System.Reactive** (v6.0.1): Reactive Extensions for .NET
    * Observable sequences
    * LINQ-style operators
    * Schedulers for time-based operations

## Cross-Platform Support

Works on:

* ✅ **Linux** (epoll)
* ✅ **macOS** (kqueue)
* ✅ **Windows** (custom implementation)

## Performance Considerations

* **Zero Polling**: WaitSet uses OS-level event notification
* **Efficient**: Operators use lazy evaluation
* **Scalable**: Can handle high-frequency events with throttling/sampling
* **Memory**: Buffering operators may accumulate events - use time limits

## Next Steps

Explore more Rx.NET operators:

* `Sample()` - Periodic sampling
* `Timeout()` - Detect missing events
* `Retry()` - Automatic retry on errors
* `Merge()` - Combine multiple observables
* `CombineLatest()` - Combine latest values
* `ObserveOn()` - Control threading

For more information, see [Rx.NET Documentation](https://github.com/dotnet/reactive).
