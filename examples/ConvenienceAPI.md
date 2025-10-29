# Convenience API Overloads

The iceoryx2 C# bindings provide convenient method overloads for common use cases, allowing developers to write simpler code while retaining access to the full low-level API when needed.

## Overview

Instead of manually managing the loan/write/send or receive/read lifecycle, you can use one-shot convenience methods that handle everything for you.

## Publisher Convenience Methods

### `Send(T value)` - Simple One-Shot Send

The most common pattern: loan, write, and send in one call.

```csharp
var publisher = service.CreatePublisher().Expect("Failed to create publisher");

// Instead of:
// var sample = publisher.Loan<MyData>().Expect("Failed to loan");
// sample.Payload = new MyData { value = 42 };
// sample.Send();
// sample.Dispose();

// Just do:
publisher.Send(new MyData { value = 42 })
    .Expect("Failed to send");
```

### `SendWith(PayloadInitializer<T>)` - Initialize by Reference

For more complex initialization where you want to modify the payload in-place:

```csharp
publisher.SendWith<MyData>((ref MyData payload) => {
    payload.field1 = 42;
    payload.field2 = 123;
    payload.timestamp = DateTime.Now.Ticks;
})
.Expect("Failed to send");
```

**Benefits:**

* Direct reference to the payload (no copying)
* Useful for structs with many fields
* Clear initialization logic

### `SendLazy(Func<T>)` - Lazy Value Creation

When value creation is expensive and should only happen if the loan succeeds:

```csharp
publisher.SendLazy(() => new MyData {
    field1 = ExpensiveComputation(),
    field2 = AnotherExpensiveOperation(),
    result = VeryExpensiveAnalysis()
})
.Expect("Failed to send");
```

**Benefits:**

* Value is only created if loan succeeds
* Avoids wasted computation if memory is exhausted
* Clean functional style

### `SendCopy(T value)` - Existing Copy-Based Send

Already exists - uses native copy path (no loan/send lifecycle):

```csharp
publisher.SendCopy(new MyData { value = 42 })
    .Expect("Failed to send copy");
```

## Subscriber Convenience Methods

### `TryReceiveValue<T>()` - Get Value Directly

Get the payload value without managing the Sample lifetime:

```csharp
var result = subscriber.TryReceiveValue<MyData>();

if (result.IsOk)
{
    var value = result.Unwrap();
    if (value.HasValue)
    {
        Console.WriteLine($"Received: {value.Value.field1}");
    }
    else
    {
        Console.WriteLine("No sample available");
    }
}
```

**Instead of:**

```csharp
var sampleResult = subscriber.Receive<MyData>();
if (sampleResult.IsOk)
{
    using var sample = sampleResult.Unwrap();
    if (sample != null)
    {
        var value = sample.Payload;
        Console.WriteLine($"Received: {value.field1}");
    }
}
```

### `ProcessSample<T>(Action<T>)` - Process with Callback

Process a sample with automatic lifetime management:

```csharp
var processed = subscriber.ProcessSample<MyData>(data => {
    Console.WriteLine($"Received: {data.field1}");
    // Do something with data
});

if (processed.IsOk && processed.Unwrap())
{
    Console.WriteLine("Sample was processed");
}
else
{
    Console.WriteLine("No sample available");
}
```

**Benefits:**

* Automatic Sample disposal
* Clean callback-based processing
* Returns bool indicating if sample was available

### `ProcessSampleAsync<T>(Action<T>, TimeSpan)` - Async Processing

Wait for and process a sample asynchronously:

```csharp
var result = await subscriber.ProcessSampleAsync<MyData>(
    data => Console.WriteLine($"Received: {data.field1}"),
    timeout: TimeSpan.FromSeconds(5));

if (result.IsOk && result.Unwrap())
{
    Console.WriteLine("Sample received and processed");
}
else
{
    Console.WriteLine("Timeout or error");
}
```

## Complete Example

```csharp
using Iceoryx2;
using System;

// Create service
var node = NodeBuilder.New().Create().Expect("Failed to create node");
var service = node.ServiceBuilder()
    .PublishSubscribe<MyData>()
    .Create("my_service")
    .Expect("Failed to create service");

// Publisher: Multiple convenience patterns
var publisher = service.CreatePublisher().Expect("Failed to create publisher");

// Pattern 1: Simple send
publisher.Send(new MyData { value = 1 }).Expect("Send failed");

// Pattern 2: Initialize with callback
publisher.SendWith<MyData>((ref MyData data) => {
    data.value = 2;
    data.timestamp = DateTime.Now.Ticks;
}).Expect("Send failed");

// Pattern 3: Lazy creation
publisher.SendLazy(() => new MyData { 
    value = ExpensiveCalculation() 
}).Expect("Send failed");

// Subscriber: Multiple convenience patterns
var subscriber = service.CreateSubscriber().Expect("Failed to create subscriber");

// Pattern 1: Get value directly
var valueResult = subscriber.TryReceiveValue<MyData>();
if (valueResult.IsOk && valueResult.Unwrap().HasValue)
{
    var data = valueResult.Unwrap().Value;
    Console.WriteLine($"Received: {data.value}");
}

// Pattern 2: Process with callback
subscriber.ProcessSample<MyData>(data => {
    Console.WriteLine($"Processing: {data.value}");
});

// Pattern 3: Async processing
await subscriber.ProcessSampleAsync<MyData>(
    data => Console.WriteLine($"Async: {data.value}"),
    TimeSpan.FromSeconds(1));

// Pattern 4: Full control (when needed)
var sampleResult = subscriber.Receive<MyData>();
if (sampleResult.IsOk)
{
    using var sample = sampleResult.Unwrap();
    if (sample != null)
    {
        var data = sample.Payload;
        // Do complex processing...
        // Maybe don't dispose immediately...
    }
}
```

## When to Use Each Pattern

### Use Convenience Methods When

* ✅ Simple, straightforward use cases
* ✅ Prototyping and initial development
* ✅ Code clarity is more important than micro-optimization
* ✅ You don't need fine-grained control

### Use Explicit API When

* 🔧 You need precise control over resource lifetimes
* 🔧 Performance-critical paths (avoiding extra allocations)
* 🔧 Complex data structures requiring incremental writes
* 🔧 You want to keep samples alive longer than the operation

## Performance Considerations

The convenience methods add minimal overhead:

* **`Send(T)`**: One extra struct copy compared to manual loan/write/send
* **`SendWith(ref T)`**: Same performance as manual approach (direct reference)
* **`SendLazy(Func<T>)`**: Slight overhead from lambda invocation
* **`TryReceiveValue<T>()`**: One extra struct copy
* **`ProcessSample<T>()`**: No extra overhead, just automatic disposal

For 99% of use cases, the convenience is worth the minimal overhead. Optimize only when profiling shows a bottleneck.

## Migration Path

You can mix and match convenience and explicit APIs:

```csharp
// Start simple during development
publisher.Send(data);

// Optimize later if needed
var sample = publisher.Loan<MyData>().Expect("Failed");
// ... complex multi-step initialization ...
sample.Send();
```

The convenience methods are **shortcuts**, not replacements. The full API remains available when you need it.
