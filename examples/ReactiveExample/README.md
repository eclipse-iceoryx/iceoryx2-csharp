# Reactive Extensions Example

This example demonstrates how to use the **iceoryx2.Reactive** library to convert iceoryx2 subscribers into reactive, declarative data streams using `IObservable<T>` and `IAsyncEnumerable<T>`.

## Overview

The Reactive Extensions (Rx.NET) pattern allows you to transform imperative polling-based code into declarative, composable data pipelines. Instead of manually polling for data, you can use LINQ-style operators to filter, transform, buffer, and throttle your data streams.

## What This Example Shows

This example demonstrates 7 different reactive patterns:

1. **Basic Observable** - Convert subscriber to `IObservable<T>`
2. **Filtering** - Use `Where()` to only emit temperatures above 28°C
3. **Transformation** - Use `Select()` to extract and transform data
4. **Buffering** - Use `Buffer()` to collect data in 2-second windows
5. **Throttling** - Use `Sample()` to throttle to 1-second intervals
6. **Distinct Changes** - Use `DistinctUntilChanged()` to only emit when values change
7. **Async Enumerable** - Use `await foreach` with `IAsyncEnumerable<T>`

## Running the Example

### Prerequisites

1. **iceoryx2 Native Library**: The example requires the iceoryx2 native library (C/C++).
2. **.NET 8.0 or .NET 9.0**: The example targets both frameworks.

### Build

```bash
dotnet build
```

### Run with Framework Selection

For .NET 8.0:

```bash
dotnet run --framework net8.0
```

For .NET 9.0:

```bash
dotnet run --framework net9.0
```

## Code Structure

### Data Structure

```csharp
struct SensorData
{
    public double Temperature;
    public double Humidity;
    public int Timestamp;
}
```

### Publisher Task

The publisher runs in the background and sends sensor data every 500ms:

```csharp
publisher.SendCopy(data).Expect("Failed to send sample");
```

### Subscriber Patterns

#### 1. Basic Observable

```csharp
subscriber.AsObservable<SensorData>()
    .Subscribe(data => Console.WriteLine($"[Basic] {data}"));
```

#### 2. Filtering

```csharp
subscriber.AsObservable<SensorData>()
    .Where(data => data.Temperature > 28.0)
    .Subscribe(data => Console.WriteLine($"[Hot] {data.Temperature:F1}°C"));
```

#### 3. Transformation

```csharp
subscriber.AsObservable<SensorData>()
    .Select(data => $"Temp: {data.Temperature:F1}°C, Humidity: {data.Humidity:F1}%")
    .Subscribe(formatted => Console.WriteLine($"[Formatted] {formatted}"));
```

#### 4. Buffering (2-second windows)

```csharp
subscriber.AsObservable<SensorData>()
    .Buffer(TimeSpan.FromSeconds(2))
    .Subscribe(buffer => {
        var avgTemp = buffer.Average(d => d.Temperature);
        Console.WriteLine($"[Buffer] Avg temp: {avgTemp:F1}°C ({buffer.Count} samples)");
    });
```

#### 5. Throttling (1-second sample)

```csharp
subscriber.AsObservable<SensorData>()
    .Sample(TimeSpan.FromSeconds(1))
    .Subscribe(data => Console.WriteLine($"[Sampled] {data}"));
```

#### 6. Distinct Until Changed

```csharp
subscriber.AsObservable<SensorData>()
    .Select(data => (int)data.Temperature)
    .DistinctUntilChanged()
    .Subscribe(temp => Console.WriteLine($"[Changed] Temperature changed to {temp}°C"));
```

#### 7. Async Enumerable

```csharp
await foreach (var data in subscriber.AsAsyncEnumerable<SensorData>())
{
    Console.WriteLine($"[AsyncEnum] {data}");
    await Task.Delay(1000);
}
```

## Key Concepts

### AsObservable<T>()

Converts a subscriber into an `IObservable<T>` that can be composed with Rx operators:

```csharp
IObservable<T> AsObservable<T>(
    TimeSpan? pollingInterval = null,
    CancellationToken cancellationToken = default)
```

* **pollingInterval**: How often to poll for data (default: 10ms)
* **cancellationToken**: Token to cancel the subscription

### AsAsyncEnumerable<T>()

Converts a subscriber into an `IAsyncEnumerable<T>` for `await foreach` patterns:

```csharp
IAsyncEnumerable<T> AsAsyncEnumerable<T>(
    TimeSpan? pollingInterval = null,
    CancellationToken cancellationToken = default)
```

## Performance Considerations

* **Polling Interval**: Lower intervals (e.g., 1ms) reduce latency but increase CPU usage. Higher intervals (e.g., 100ms) reduce CPU usage but increase latency.
* **Default**: 10ms is a reasonable default for most use cases.
* **Resource Cleanup**: Always dispose subscriptions when done to stop polling tasks.

## Disposing Subscriptions

All subscriptions are disposable. Use `using` to ensure cleanup:

```csharp
using var subscription = subscriber.AsObservable<SensorData>()
    .Where(data => data.Temperature > 28.0)
    .Subscribe(data => Console.WriteLine(data));

// subscription automatically disposed when out of scope
```

## Next Steps

* Try modifying the filters and transformations
* Experiment with different Rx operators (`Throttle`, `Debounce`, `Merge`, etc.)
* Adjust the polling interval to see latency vs. CPU trade-offs
* Use `CancellationTokenSource` for graceful shutdown

## References

* [iceoryx2.Reactive README](../../src/Iceoryx2.Reactive/README.md)
* [Rx.NET Documentation](https://github.com/dotnet/reactive)
* [IAsyncEnumerable<T> Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/async-streams)
