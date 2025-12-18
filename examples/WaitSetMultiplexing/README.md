# WaitSet Event Multiplexing Example

This example demonstrates how to use the `WaitSet` API for efficient
event-driven communication without polling, using modern C# async/await patterns.
The WaitSet uses OS-level primitives (epoll on Linux, kqueue on macOS) to
monitor multiple event sources simultaneously.

> **Note**: This example requires the iceoryx2 C library to be built and
> available. See the main iceoryx2 documentation for build instructions.

## Overview

The example consists of two programs:

1. **Waiter**: Asynchronously monitors multiple event services using a WaitSet
2. **Notifier**: Asynchronously sends events to a specific service

## Key Concepts

### WaitSet with Async/Await

* Event multiplexing mechanism that blocks until events arrive
* No CPU polling - uses OS-level event notification
* **Async/Await Integration**: WaitSet runs in background Task for non-blocking operation
* Can monitor multiple listeners, deadlines, and periodic intervals
* Supports graceful shutdown via signal handling (Ctrl+C) with CancellationToken

### Critical Pattern: Event Consumption

The callback **must consume ALL pending events** to avoid busy loops:

```csharp
do
{
    var eventOpt = listener.TryWaitOne();
    if (eventOpt.HasValue)
    {
        // Process event
        Console.WriteLine($"Event: {eventOpt.Value}");
    }
    else
    {
        break; // No more events
    }
} while (true);
```

**Why?** The WaitSet wakes when data is available. If events aren't consumed,
the file descriptor remains ready and the WaitSet immediately wakes again,
creating a busy loop.

## Building

```bash
dotnet build
```

## Running

> **Note**: Since the project targets multiple frameworks (.NET 8.0 and
> .NET 9.0), you must specify which framework to use with `--framework`.

### Terminal 1: Start the Waiter

Monitor two event services:

```bash
dotnet run --framework net9.0 -- wait service_a service_b
```

Or using .NET 8.0:

```bash
dotnet run --framework net8.0 -- wait service_a service_b
```

Output:

```text
Waiting on services: 'service_a' and 'service_b'
```

### Terminal 2: Send Events to Service A

```bash
dotnet run --framework net9.0 -- notify 123 service_a
```

Output (in Terminal 1):

```text
[service: 'service_a'] event received with id: 123
[service: 'service_a'] event received with id: 123
...
```

### Terminal 3: Send Events to Service B

```bash
dotnet run --framework net9.0 -- notify 456 service_b
```

Output (in Terminal 1):

```text
[service: 'service_b'] event received with id: 456
[service: 'service_b'] event received with id: 456
...
```

## Signal Handling

The waiter uses `SignalHandlingMode.TerminationAndInterrupt` to handle:

* `SIGTERM` (Ctrl+C on Unix/Linux/macOS)
* `SIGINT` (interrupt signal)

Press Ctrl+C to gracefully shut down:

```text
^C
WaitSet completed with result: TerminationRequest
```

## Architecture

```text
┌─────────────────────────────────────────┐
│           Waiter Process                │
│  ┌───────────────────────────────────┐  │
│  │         WaitSet                   │  │
│  │  ┌─────────────┐  ┌─────────────┐│  │
│  │  │  Listener A │  │  Listener B ││  │
│  │  └──────┬──────┘  └──────┬──────┘│  │
│  └─────────┼────────────────┼───────┘  │
└────────────┼────────────────┼──────────┘
             │                │
             │ Events         │ Events
             │                │
┌────────────┼────────┐  ┌────┼──────────┐
│  ┌─────────▼──────┐ │  │ ┌──▼────────┐ │
│  │  Notifier      │ │  │ │ Notifier  │ │
│  │  (Event ID 123)│ │  │ │(Event ID  │ │
│  └────────────────┘ │  │ │   456)    │ │
│   Notifier Process  │  │ └───────────┘ │
│   (service_a)       │  │  Notifier     │
└─────────────────────┘  │  Process      │
                         │  (service_b)  │
                         └───────────────┘
```

## API Usage

### Creating a WaitSet with Async Pattern

```csharp
var waitset = WaitSetBuilder.New()
    .SignalHandling(SignalHandlingMode.TerminationAndInterrupt)
    .Create()
    .Expect("Failed to create WaitSet");

// Run WaitSet in background task for non-blocking async operation
var waitTask = Task.Run(() => waitset.WaitAndProcess(OnEvent));

// Await completion
var result = await waitTask;
```

### Attaching Listeners

```csharp
var guard = waitset.AttachNotification(listener)
    .Expect("Failed to attach listener");
```

**Important**: Keep the `WaitSetGuard` alive! When disposed, the attachment is
automatically removed.

### Event Processing Callback

```csharp
CallbackProgression OnEvent(WaitSetAttachmentId attachmentId)
{
    if (attachmentId.HasEventFrom(guard))
    {
        // Process events from this guard
        var eventResult = listener.TryWait();
        // ...
    }
    return CallbackProgression.Continue; // or Stop
}
```

### Async Notifier with CancellationToken

```csharp
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
        notifier.Notify(eventId).Expect("Failed to notify");
        await Task.Delay(1000, cts.Token);
    }
}
catch (OperationCanceledException)
{
    // Expected when Ctrl+C is pressed
}
```

### Other Attachment Types

**Deadline**: Wake on events OR timeout

```csharp
var guard = waitset.AttachDeadline(listener, TimeSpan.FromSeconds(5))
    .Expect("Failed to attach with deadline");

if (attachmentId.HasMissedDeadline(guard))
{
    Console.WriteLine("Deadline missed!");
}
```

**Interval**: Periodic wake-ups

```csharp
var guard = waitset.AttachInterval(TimeSpan.FromSeconds(1))
    .Expect("Failed to attach interval");
```

## Cross-Platform Support

The WaitSet automatically uses the best mechanism for each OS:

* **Linux**: epoll
* **macOS**: kqueue
* **Windows**: Custom implementation

No code changes needed - it just works!

## Cleanup

All resources use RAII patterns with `IDisposable`:

```csharp
using var waitset = WaitSetBuilder.New().Create().Expect("...");
using var guard = waitset.AttachNotification(listener).Expect("...");
// Automatic cleanup on scope exit
```

## Performance

* **Zero polling**: CPU usage is near zero when idle
* **Low latency**: OS-level event notification wakes immediately
* **Scalable**: Can monitor many event sources efficiently
* **OS-optimized**: Uses native primitives for best performance
* **Async/Await**: Non-blocking async operations allow efficient concurrent processing

## Benefits of Async/Await Pattern

1. **Non-blocking**: Main thread remains responsive while WaitSet runs in background
2. **Cancellation**: Proper cancellation token support for graceful shutdown
3. **Modern C#**: Idiomatic async/await patterns familiar to .NET developers
4. **Integration**: Easy to integrate with other async operations in your application
5. **Resource Efficiency**: Task-based parallelism with minimal overhead
