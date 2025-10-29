# Async Publish-Subscribe Example

This example demonstrates the async/await functionality in the iceoryx2 C# bindings for publish-subscribe communication.

## Features Demonstrated

* ✅ Async publisher using `await Task.Delay()` instead of `Thread.Sleep()`
* ✅ Async subscriber with timeout using `ReceiveAsync(TimeSpan, CancellationToken)`
* ✅ Async subscriber polling indefinitely using `ReceiveAsync(CancellationToken)`
* ✅ Multiple concurrent subscribers processing data in parallel
* ✅ Proper cancellation support with `CancellationToken`
* ✅ Graceful shutdown with Ctrl+C handling

**Note:** Subscribers use polling (every 10ms) since the native API doesn't provide blocking receive. However, the async implementation yields to the thread pool efficiently, making it suitable for async scenarios without blocking threads.

## Building

```bash
dotnet build
```

## Running

### Start a Publisher

The publisher sends incrementing counter values every second:

```bash
dotnet run publisher
```

Output:

```
iceoryx2 C# Async Publish-Subscribe Example
============================================

Starting Async Publisher...

Node created: csharp_async_publisher
Service opened
Publisher created
Press Ctrl+C to stop

[10:30:45.123] Sent: 0
[10:30:46.124] Sent: 1
[10:30:47.125] Sent: 2
...
```

### Start a Subscriber (with timeout)

The subscriber waits up to 5 seconds for each sample:

```bash
# In another terminal
dotnet run subscriber
```

Output:

```
Starting Async Subscriber (with 5s timeout)...

Node created: csharp_async_subscriber
Service opened
Subscriber created
Waiting for samples (async with timeout)...
Press Ctrl+C to stop

[10:30:46.125] Received: 1
[10:30:47.126] Received: 2
[10:30:48.127] Received: 3
...
```

### Start a Blocking Subscriber

The subscriber polls asynchronously until data arrives (no timeout):

```bash
# In another terminal
dotnet run blocking
```

Output:

```
Starting Async Subscriber (polling until data)...

Node created: csharp_async_blocking_subscriber
Service opened
Subscriber created
Waiting for samples (polling async, no timeout)...
Note: Polls every 10ms but yields to thread pool efficiently
Press Ctrl+C to stop

[10:30:47.128] Received: 2
[10:30:48.129] Received: 3
...
```

### Start Multiple Concurrent Subscribers

Demonstrates multiple subscribers processing data concurrently:

```bash
# In another terminal
dotnet run multi
```

Output:

```
Starting 3 Concurrent Async Subscribers...

Created 3 subscribers
Each subscriber will process data concurrently
Press Ctrl+C to stop

[10:30:48.130] [Sub-1] Received: 3
[10:30:48.130] [Sub-2] Received: 3
[10:30:48.130] [Sub-3] Received: 3
[10:30:49.131] [Sub-1] Received: 4
[10:30:49.131] [Sub-2] Received: 4
[10:30:49.131] [Sub-3] Received: 4
...
```

## Key Differences from Sync Example

### Publisher

**Synchronous:**

```csharp
while (true)
{
    // ... send sample
    Thread.Sleep(1000);  // Blocks thread
}
```

**Asynchronous:**

```csharp
while (!cancellationToken.IsCancellationRequested)
{
    // ... send sample
    await Task.Delay(1000, cancellationToken);  // Yields to thread pool
}
```

### Subscriber

**Synchronous:**

```csharp
while (true)
{
    var sample = subscriber.Receive<int>();
    // ... process sample
    Thread.Sleep(100);  // Blocks thread
}
```

**Asynchronous with timeout:**

```csharp
while (!cancellationToken.IsCancellationRequested)
{
    // Waits up to 5 seconds asynchronously
    var result = await subscriber.ReceiveAsync<int>(
        TimeSpan.FromSeconds(5), 
        cancellationToken);
    // ... process result
}
```

**Asynchronous blocking:**

```csharp
while (!cancellationToken.IsCancellationRequested)
{
    // Polls indefinitely (but can be cancelled)
    // Yields every 10ms to avoid blocking threads
    var result = await subscriber.ReceiveAsync<int>(cancellationToken);
    // ... process result
}
```

## Benefits of Async Version

1. **Better Thread Pool Utilization**
   * No blocking of threads during waits
   * Thread pool can reuse threads for other work

2. **Proper Cancellation**
   * All operations support `CancellationToken`
   * Clean shutdown with Ctrl+C

3. **Composability**
   * Can use `Task.WhenAll()` for concurrent operations
   * Natural integration with other async code

4. **Scalability**
   * Multiple subscribers can run efficiently in parallel
   * No thread-per-subscriber overhead

## Technical Details

### Polling Interval

The async subscriber methods poll every 10ms when waiting for data:

* **CPU Impact**: Very low (Task.Delay yields to thread pool)
* **Latency**: ~5ms average, ~10ms maximum
* **Acceptable for**: Most IPC scenarios

### Cancellation

All async methods check the cancellation token:

* On each polling iteration
* Before long-running operations
* Throws `OperationCanceledException` when cancelled

### Thread Safety

All iceoryx2 objects are designed to be used from a single thread. If you need to use them from multiple threads, you must provide your own synchronization.

## See Also

* [ProgramAsync.cs](../PublishSubscribe/ProgramAsync.cs) - Async helper methods (not standalone)
* [../RequestResponse/ProgramAsync.cs](../RequestResponse/ProgramAsync.cs) - Async request-response example
* [../../ASYNC_SUPPORT.md](../../ASYNC_SUPPORT.md) - Detailed async/await documentation
