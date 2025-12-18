# Task-to-Task Communication Example

This example demonstrates how to use iceoryx2 for communication between async
tasks within a single executable, showcasing zero-copy shared memory IPC.

## Overview

The example simulates a sensor data processing pipeline with four concurrent tasks:

1. **Sensor Task 1** - Publishes sensor data every 500ms
2. **Sensor Task 2** - Publishes sensor data every 700ms
3. **Processor Task** - Aggregates sensor data and publishes processed results
   every 2 seconds
4. **Display Task** - Displays the aggregated results

All communication uses zero-copy shared memory access via `GetPayloadRef()` and `GetPayloadRefReadOnly()`.

## Architecture

```text
┌─────────────┐
│  Sensor 1   │──┐
└─────────────┘  │
                 ├──► ┌─────────────┐      ┌─────────────┐
┌─────────────┐  │    │  Processor  │─────►│   Display   │
│  Sensor 2   │──┘    └─────────────┘      └─────────────┘
└─────────────┘
   (500/700ms)         (every 2s)           (real-time)
```

## Key Features

* ✅ **Zero-Copy Access** - Direct memory access using `ref` returns
* ✅ **Async/Await** - Modern async patterns with `CancellationToken`
* ✅ **Multiple Tasks** - Four concurrent tasks in one process
* ✅ **Data Aggregation** - Processor task demonstrates data processing
* ✅ **Graceful Shutdown** - Ctrl+C handling with proper cleanup

## Running the Example

```bash
cd examples/TaskCommunication
dotnet run
```

Press `Ctrl+C` to stop all tasks gracefully.

## Code Highlights

### Zero-Copy Write (Sensor)

```csharp
var loanResult = publisher.Loan<SensorData>();
using var sample = loanResult.Unwrap();

// Direct zero-copy write to shared memory
ref var data = ref sample.GetPayloadRef();
data.Temperature = 20.0 + random.NextDouble() * 10.0;
data.Pressure = 1000.0 + random.NextDouble() * 50.0;

sample.Send();
```

### Zero-Copy Read (Processor)

```csharp
var sample = receiveResult.Unwrap();
using (sample)
{
    // Direct zero-copy read from shared memory
    ref readonly var data = ref sample.GetPayloadRefReadOnly();
    samples.Add(data);
}
```

## Expected Output

```text
===========================================
Task-to-Task Communication Example
===========================================

[Sensor 1] Started
[Sensor 2] Started
[Processor] Started
[Display] Started
All tasks started. Press Ctrl+C to stop.

[Sensor 1] Published: T=25.34°C, P=1023.45hPa
[Sensor 2] Published: T=22.78°C, P=1015.67hPa
[Processor] Published aggregated data from 5 samples

╔════════════════════════════════════════╗
║      AGGREGATED SENSOR DATA            ║
╠════════════════════════════════════════╣
║ Timestamp:       2450 ms               ║
║ Avg Temp:        24.12 °C              ║
║ Avg Pressure:  1019.34 hPa             ║
║ Sample Count:        5                 ║
╚════════════════════════════════════════╝
```

## Learning Points

1. **Single Executable IPC** - iceoryx2 works within a single process for task communication
2. **Zero-Copy Performance** - No marshaling overhead when accessing shared memory
3. **Async Patterns** - Proper use of async/await with cancellation tokens
4. **Resource Management** - Automatic cleanup with `using` statements
5. **Data Pipeline** - Demonstrates a realistic sensor → processor → display pipeline
