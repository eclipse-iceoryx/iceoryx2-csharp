# WaitSet IAsyncEnumerable Example

This example demonstrates the modern `IAsyncEnumerable<WaitSetEvent>` API for processing WaitSet events. This approach provides significant advantages over the traditional callback pattern.

## The Problem with Callbacks

The traditional WaitSet callback API has a critical usability issue: **the busy-loop pitfall**. If developers don't consume all pending events in their callback, the WaitSet enters a busy-loop that wastes CPU cycles. From the WaitSet README:

> **⚠️ IMPORTANT:** The callback **MUST** consume all pending events (by calling `TryWait()` until it returns `None`) to avoid a busy-loop!

This is a classic "pit of failure"—a pattern that's easy to get wrong with severe performance consequences.

## The Solution: IAsyncEnumerable

The new `Events()` method provides a modern, async-friendly API that eliminates this pitfall entirely:

```csharp
public async IAsyncEnumerable<WaitSetEvent> Events(CancellationToken cancellationToken = default)
```

### Benefits

1. **Eliminates the Busy-Loop Pitfall**: The library correctly handles event consumption internally
2. **Simplifies User Code**: Replaces complex callback state management with standard `await foreach`
3. **Integrates with Async LINQ**: Use operators from `System.Linq.Async` (`Where`, `Select`, `Buffer`, etc.)
4. **Proper Cancellation Support**: First-class `CancellationToken` integration

## API Comparison

### Before (Callback Pattern)

```csharp
// Complex setup with separate context class
public class CallbackContext
{
    public Listener Listener1 { get; set; }
    public Listener Listener2 { get; set; }
    public WaitSetGuard Guard1 { get; set; }
    public WaitSetGuard Guard2 { get; set; }
}

var context = new CallbackContext { ... };

// Callback function with manual loop - easy to forget to consume all events!
var waitTask = Task.Run(() => waitset.WaitAndProcess(attachmentId =>
{
    if (attachmentId.HasEventFrom(context.Guard1))
    {
        // Must remember to call TryWait() until None!
        while (true)
        {
            var eventId = context.Listener1.TryWait().Unwrap();
            if (!eventId.HasValue) break; // Easy to forget this!
            ProcessEvent(eventId.Value);
        }
    }
    return CallbackProgression.Continue;
}));
```

### After (IAsyncEnumerable Pattern)

```csharp
// Simple, clean, and safe!
var guard1 = waitSet.AttachNotification(listener1).Unwrap();
var guard2 = waitSet.AttachNotification(listener2).Unwrap();

await foreach (var evt in waitSet.Events(cancellationToken))
{
    if (evt.IsFrom(guard1))
    {
        var eventId = listener1.TryWait().Unwrap();
        if (eventId.HasValue)
        {
            Console.WriteLine($"Event: {eventId.Value}");
        }
    }
    else if (evt.IsFrom(guard2))
    {
        var eventId = listener2.TryWait().Unwrap();
        if (eventId.HasValue)
        {
            Console.WriteLine($"Event: {eventId.Value}");
        }
    }
}
```

## Advanced Usage

### Time-Limited Event Processing

Process events for a specific duration:

```csharp
await foreach (var evt in waitSet.Events(TimeSpan.FromSeconds(10), cancellationToken))
{
    // Automatically stops after 10 seconds
    ProcessEvent(evt);
}
```

### Async LINQ Integration

Use `System.Linq.Async` for powerful event stream manipulation:

```csharp
using System.Linq;

// Take first 10 events only
await foreach (var evt in waitSet.Events(ct).Take(10))
{
    ProcessEvent(evt);
}

// Buffer events in batches
await foreach (var batch in waitSet.Events(ct).Buffer(5))
{
    ProcessBatch(batch);
}

// Filter events
await foreach (var evt in waitSet.Events(ct).Where(e => e.IsFrom(guard1)))
{
    ProcessService1Event(evt);
}
```

## Migration Guide

### Step 1: Replace Callback with Async Foreach

**Old:**

```csharp
var result = waitSet.WaitAndProcess(attachmentId =>
{
    // ... complex callback logic ...
    return CallbackProgression.Continue;
});
```

**New:**

```csharp
await foreach (var evt in waitSet.Events(cancellationToken))
{
    // ... simple event handling ...
}
```

### Step 2: Use Guards for Comparison

Store the guards returned from `Attach` methods:

```csharp
var guard1 = waitSet.AttachNotification(listener1).Unwrap();
var guard2 = waitSet.AttachNotification(listener2).Unwrap();
```

Use `evt.IsFrom(guard)` to identify the source:

```csharp
if (evt.IsFrom(guard1))
{
    // Handle listener1 event
}
```

### Step 3: Handle Cancellation

Pass a `CancellationToken` to stop event processing cleanly:

```csharp
using var cts = new CancellationTokenSource();

// Cancel after 30 seconds
cts.CancelAfter(TimeSpan.FromSeconds(30));

await foreach (var evt in waitSet.Events(cts.Token))
{
    // Will stop after 30 seconds or when cts.Cancel() is called
}
```

## Running the Example

```bash
dotnet run
```

## Expected Output

```
=== WaitSet IAsyncEnumerable Demo ===

✓ WaitSet created with 2 listener attachments
  Capacity: 32, Length: 2

Started async event processing loop...

Sending events...

  → Sent event 0 to service 1
  → Sent event 100 to service 2
[Service 1] Received event: 0
[Service 2] Received event: 100
  → Sent event 1 to service 1
  → Sent event 101 to service 2
[Service 1] Received event: 1
[Service 2] Received event: 101
...

Shutting down...

✓ Event processing stopped gracefully

=== Demo Complete ===
```

## Key Takeaways

* ✅ **Use `Events()` for new code** - it's safer and more idiomatic
* ✅ **No more busy-loop risk** - the library handles event consumption
* ✅ **Clean async/await pattern** - integrates naturally with modern C#
* ✅ **Powerful composition** - works with async LINQ operators
* ⚠️ **Callback API still available** - for advanced scenarios requiring fine-grained control

The `IAsyncEnumerable` API represents the recommended approach for WaitSet event processing in modern C# applications.
