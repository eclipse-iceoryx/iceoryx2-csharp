# Reactive Event Example

This example demonstrates **event-driven Observable** usage with iceoryx2's Listener and Notifier using the `Iceoryx2.Reactive` library.

## Key Concepts

### Event-Driven vs Polling-Based

This example showcases **truly event-driven** reactive programming:

* **Event-based (Listener/Notifier)** = Truly asynchronous with WaitSet (epoll/kqueue)
    * No polling loops
    * Efficient CPU usage
    * Immediate event notification
    * Used in this example

* **Pub/Sub (Subscriber/Publisher)** = Polling-based architecture (see `ReactiveExample`)
    * Uses periodic polling
    * Higher latency
    * More CPU overhead

### Architecture

```
Notifier → [Event Service] → Listener → WaitSet → Observable → Rx Operators
           (Event IDs)                  (epoll/kqueue)
```

## Building

```bash
cd /Users/patdhlk/src/patdhlk/iceoryx2/iceoryx2-ffi/csharp
dotnet build examples/ReactiveEventExample/ReactiveEventExample.csproj
```

## Running

### Terminal 1: Start the Listener

```bash
cd examples/ReactiveEventExample
dotnet run --framework net9.0 -- listener events
```

The listener will demonstrate various Rx operators:

1. **Basic Observable** - Raw event stream
2. **Filter Events** - Only alert events (ID 10-19)
3. **Transform Events** - Event summaries with metadata
4. **Count Events** - Group by type in time windows
5. **Critical Events** - High-priority events only
6. **Throttle** - Debounce frequent events
7. **Event Patterns** - Detect event sequences
8. **Async Enumerable** - await foreach pattern
9. **Deadline Monitoring** - Detect missing events

### Terminal 2: Start the Notifier

```bash
cd examples/ReactiveEventExample
dotnet run --framework net9.0 -- notifier events
```

The notifier will send various event types:

* System events (startup, shutdown)
* Alerts (warning, error, critical)
* Health monitoring (heartbeat)
* Data events (data ready)
* User actions

## Event Types

The example uses these event IDs:

| Event ID | Name              | Category | Severity | Frequency |
|----------|-------------------|----------|----------|-----------|
| 1        | SystemStartup     | System   | Info     | Rare      |
| 2        | SystemShutdown    | System   | Info     | Rare      |
| 10       | WarningAlert      | Alert    | Warning  | Medium    |
| 11       | ErrorAlert        | Alert    | Error    | Medium    |
| 12       | CriticalAlert     | Alert    | Critical | Low       |
| 20       | PerformanceMetric | Metric   | Info     | Medium    |
| 30       | HeartBeat         | Health   | Info     | High      |
| 40       | DataReady         | Data     | Info     | Medium    |
| 50       | UserAction        | User     | Info     | Medium    |

## Demonstrated Rx Operators

### Basic Operators

* `Subscribe()` - Basic event consumption
* `Where()` - Filter events by condition
* `Select()` - Transform events to different types

### Time-Based Operators

* `Buffer()` - Collect events in time windows
* `Sample()` - Take latest event at intervals
* `Throttle()` - Suppress rapid events

### Advanced Patterns

* `GroupBy()` - Group events by property
* Custom event pattern detection
* Deadline monitoring with timeout
* `AsAsyncEnumerable()` - Convert to async enumerable

## Performance Notes

### WaitSet Efficiency

The `ListenerExtensions.AsObservable()` method uses WaitSet internally, which means:

✅ **Efficient**:

* No polling loops (unlike `SubscriberExtensions`)
* Events trigger immediately via kernel notification (epoll/kqueue)
* Minimal CPU usage when idle
* Multiple listeners can be multiplexed efficiently

⚠️ **Important**:

* The internal implementation consumes **all pending events** in a loop to avoid busy-waiting
* This is correct behavior for event streams where you want to process all queued events

### When to Use Events vs Pub/Sub

**Use Events (Listener/Notifier)** when:

* You need lightweight notifications (just IDs)
* Event-driven architecture is essential
* Minimal latency is important
* You're building control/coordination systems

**Use Pub/Sub (Subscriber/Publisher)** when:

* You need to transfer data payloads
* Data volume is more important than latency
* You're building data streaming systems

## Code Highlights

### Creating Event-Driven Observable

```csharp
// Basic event observable with WaitSet
listener.AsObservable(cancellationToken: cts.Token)
    .Subscribe(eventId => Console.WriteLine($"Event: {eventId.Value}"));

// With deadline monitoring
listener.AsObservable(
        deadline: TimeSpan.FromSeconds(2),
        cancellationToken: cts.Token)
    .Subscribe(eventId => Console.WriteLine($"Event in time: {eventId.Value}"));
```

### Filtering and Transforming

```csharp
// Filter alert events only
listener.AsObservable(cancellationToken: cts.Token)
    .Where(eventId => eventId.Value is >= 10 and < 20)
    .Subscribe(eventId => Console.WriteLine($"Alert: {eventId.Value}"));

// Transform to rich event data
listener.AsObservable(cancellationToken: cts.Token)
    .Select(eventId => new 
    { 
        Event = GetEventName(eventId),
        Category = GetEventCategory(eventId),
        Severity = GetEventSeverity(eventId),
        Timestamp = DateTime.Now
    })
    .Subscribe(summary => Console.WriteLine(summary));
```

### Time-Based Processing

```csharp
// Count events in 3-second windows
listener.AsObservable(cancellationToken: cts.Token)
    .Buffer(TimeSpan.FromSeconds(3))
    .Where(batch => batch.Count > 0)
    .Subscribe(batch => 
    {
        Console.WriteLine($"Received {batch.Count} events");
        foreach (var group in batch.GroupBy(GetEventName))
            Console.WriteLine($"  {group.Key}: {group.Count()}");
    });
```

### Async Enumerable Pattern

```csharp
// Use await foreach for async iteration
await foreach (var eventId in listener.AsAsyncEnumerable(cancellationToken: cts.Token))
{
    Console.WriteLine($"Event: {GetEventName(eventId)}");
    if (++count >= 5) break;
}
```

## See Also

* **Event Example** (`examples/Event`) - Basic Listener/Notifier without Rx
* **ReactiveExample** (`examples/ReactiveExample`) - Polling-based pub/sub with Rx
* **ObservableWaitSet** (`examples/ObservableWaitSet`) - Low-level WaitSet usage
* **WaitSetMultiplexing** (`examples/WaitSetMultiplexing`) - WaitSet multiplexing patterns
