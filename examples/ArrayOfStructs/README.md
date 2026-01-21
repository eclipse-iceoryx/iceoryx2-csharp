# Array of Structs Example

This example demonstrates how to send and receive arrays of custom structs using
iceoryx2's zero-copy inter-process communication. It shows how to efficiently
transmit collections of structured data without serialization overhead.

> **Note**: This example requires the iceoryx2 C library to be built and
> available. See the main iceoryx2 documentation for build instructions.

## Overview

The example demonstrates:

1. **Custom Struct Definition**: How to define C# structs for IPC with proper
   memory layout
2. **Array/Slice Publishing**: Sending variable-length arrays of structs via `LoanSlice()`
3. **Zero-Copy Transfer**: No serialization - direct memory mapping between processes
4. **Multiple Data Types**: Examples with both particle simulation and sensor data

## Key Concepts

### Struct Requirements for IPC

For structs to work with iceoryx2, they must:

* Use `[StructLayout(LayoutKind.Sequential)]` for predictable memory layout
* Be marked with `[Iox2Type("TypeName")]` attribute for type identification
* Implement `unmanaged` constraint (no reference types)
* Have fixed size (no variable-length fields)

```csharp
[StructLayout(LayoutKind.Sequential)]
[Iox2Type("Particle")]
public struct Particle
{
    public float X;
    public float Y;
    public float Z;
    public float Velocity;
    public int Id;
}
```

### Zero-Copy Array Transfer

Instead of serializing arrays, iceoryx2 uses shared memory:

1. **Loan Slice**: Allocate shared memory for array
2. **Copy Data**: Write struct array to shared memory
3. **Send**: Transfer ownership to subscribers
4. **Receive**: Access array directly in shared memory

**Benefits:**

* No serialization/deserialization overhead
* Efficient for large arrays
* Predictable latency
* Low CPU usage

## Building

```bash
dotnet build
```

## Running

> **Note**: Since the project targets multiple frameworks (.NET 8.0 and
> .NET 9.0), you must specify which framework to use with `--framework`.

### Example 1: Particle Array

Demonstrates sending arrays of particle data (physics simulation style).

#### Terminal 1: Start Subscriber

```bash
dotnet run --framework net9.0 -- subscriber particle
```

Or with .NET 8.0:

```bash
dotnet run --framework net8.0 -- subscriber particle
```

Output:

```text
📥 [Subscriber] Type: Particle
   Service: array-particle-service
   Struct size: 20 bytes

✅ Subscriber created successfully.
Waiting for data...
```

#### Terminal 2: Start Publisher

```bash
dotnet run --framework net9.0 -- publisher particle
```

Output:

```text
📤 [Publisher] Type: Particle
   Service: array-particle-service
   Struct size: 20 bytes

✅ Publisher created successfully.
Press Ctrl+C to stop.

📦 Iteration 0: Sending array of 5 Particle structs
   [0] Particle[0] { pos: (48.41, 30.60, 40.21), vel: 7.55 }
   [1] Particle[1] { pos: (27.18, 72.93, 95.77), vel: 7.08 }
   [2] Particle[2] { pos: (67.59, 39.48, 54.39), vel: 4.01 }
   [3] Particle[3] { pos: (78.47, 77.34, 10.89), vel: 4.85 }
   [4] Particle[4] { pos: (53.83, 36.39, 92.31), vel: 5.94 }
✅ Sent successfully!
```

**Subscriber Output:**

```text
📬 Received array with 5 elements:
   [0] Particle[0] { pos: (48.41, 30.60, 40.21), vel: 7.55 }
   [1] Particle[1] { pos: (27.18, 72.93, 95.77), vel: 7.08 }
   [2] Particle[2] { pos: (67.59, 39.48, 54.39), vel: 4.01 }
   [3] Particle[3] { pos: (78.47, 77.34, 10.89), vel: 4.85 }
   [4] Particle[4] { pos: (53.83, 36.39, 92.31), vel: 5.94 }
✅ Total arrays received: 1
```

### Example 2: Sensor Data Array

Demonstrates sending arrays of sensor readings with timestamps.

#### Terminal 1: Start Subscriber

```bash
dotnet run --framework net9.0 -- subscriber sensor
```

#### Terminal 2: Start Publisher

```bash
dotnet run --framework net9.0 -- publisher sensor
```

Output:

```text
📦 Iteration 0: Sending array of 5 SensorReading structs
   [0] Sensor[100] @ 1703001234500: Temp=22.4°C, Press=1015.3hPa, Hum=48.2%
   [1] Sensor[101] @ 1703001234600: Temp=23.1°C, Press=1018.7hPa, Hum=52.9%
   [2] Sensor[102] @ 1703001234700: Temp=21.8°C, Press=1012.4hPa, Hum=45.7%
   ...
✅ Sent successfully!
```

## API Usage

### Creating a Publisher for Arrays

```csharp
// Open service for struct type T
using var service = node.ServiceBuilder()
    .PublishSubscribe<T>()
    .Open(serviceName)
    .Expect("Failed to open service");

// Create publisher
using var publisher = service.CreatePublisher()
    .Expect("Failed to create publisher");
```

### Sending Array Data

```csharp
var dataArray = new Particle[10];
// ... populate array ...

// Loan a slice (allocates shared memory for array)
var sample = publisher.LoanSlice((ulong)dataArray.Length)
    .Expect("Failed to loan slice");

// Get payload as span and copy data
var payload = sample.Payload;
for (int i = 0; i < dataArray.Length; i++)
{
    payload[i] = dataArray[i];
}

// Send the sample
sample.Send().Expect("Failed to send");
```

### Receiving Array Data

```csharp
// Create subscriber
using var subscriber = service.SubscriberBuilder()
    .Create()
    .Expect("Failed to create subscriber");

// Receive sample
var receiveResult = subscriber.Receive<T>();
if (receiveResult.IsOk)
{
    var sample = receiveResult.Unwrap();
    if (sample != null)
    {
        using (sample)
        {
            var payload = sample.Payload; // Span<T>

            // Process each element
            for (int i = 0; i < payload.Length; i++)
            {
                Console.WriteLine($"Element {i}: {payload[i]}");
            }
        }
    }
}
```

## Data Types

### Particle Struct

Represents a particle in a physics simulation:

```csharp
[StructLayout(LayoutKind.Sequential)]
[Iox2Type("Particle")]
public struct Particle
{
    public float X, Y, Z;      // Position
    public float Velocity;      // Speed
    public int Id;             // Unique identifier
}
```

**Size:** 20 bytes (3 floats + 1 float + 1 int)

**Use Case:** Physics simulations, particle systems, spatial data

### SensorReading Struct

Represents environmental sensor data:

```csharp
[StructLayout(LayoutKind.Sequential)]
[Iox2Type("SensorReading")]
public struct SensorReading
{
    public long Timestamp;     // Unix timestamp (ms)
    public float Temperature;  // °C
    public float Pressure;     // hPa
    public float Humidity;     // %
    public ushort SensorId;    // Sensor identifier
}
```

**Size:** 26 bytes (1 long + 3 floats + 1 ushort + padding)

**Use Case:** IoT sensors, environmental monitoring, telemetry

## Performance Characteristics

### Memory Efficiency

* **Zero-Copy**: Data is directly mapped into shared memory
* **No Serialization**: Binary layout is used as-is
* **Shared Memory**: Single allocation shared between publisher and all subscribers

### Array Size Flexibility

* Array size can vary per message (5-10 elements in this example)
* Each `LoanSlice()` call allocates exactly the requested size
* No overhead for unused capacity

### Latency

* **Predictable**: No serialization overhead
* **Low**: Direct memory access, minimal copying
* **Constant**: Performance independent of struct complexity

## Best Practices

### 1. Memory Layout

Always use `StructLayout(LayoutKind.Sequential)` for predictable cross-process compatibility:

```csharp
[StructLayout(LayoutKind.Sequential)]
public struct MyData { ... }
```

### 2. Type Safety

Use the `[Iox2Type]` attribute to ensure publisher and subscriber use matching types:

```csharp
[Iox2Type("Particle")]  // Type identifier
public struct Particle { ... }
```

### 3. Resource Management

Always dispose samples to release shared memory:

```csharp
using (var sample = receiveResult.Unwrap())
{
    // Process sample
}  // Automatically disposed
```

### 4. Error Handling

Check results at each step:

```csharp
var sample = publisher.LoanSlice(count)
    .Expect("Failed to loan slice");

sample.Send()
    .Expect("Failed to send");
```

## Limitations

* Structs must be `unmanaged` (no reference types like strings, arrays, classes)
* Arrays have fixed element type (cannot mix types in one array)
* Array size must be known when loaning the slice
* Structs should avoid padding for optimal memory usage

## Use Cases

This pattern is ideal for:

* **Physics Simulations**: Particle positions, velocities, forces
* **Sensor Networks**: Batched sensor readings from multiple devices
* **Computer Vision**: Feature points, detection results
* **Telemetry**: System metrics, performance counters
* **Robotics**: Joint states, trajectory points
* **Game Development**: Entity updates, collision data

## Cross-Platform Support

Works on:

* ✅ **Linux**
* ✅ **macOS**
* ✅ **Windows**

Memory layout is guaranteed consistent across platforms due to `StructLayout(LayoutKind.Sequential)`.

## Cleanup

All resources use RAII patterns with `IDisposable`:

```csharp
using var node = NodeBuilder.New().Create().Expect("...");
using var service = node.ServiceBuilder()...;
using var publisher = service.CreatePublisher()...;
// Automatic cleanup on scope exit
```

## Related Examples

* **PublishSubscribe**: Basic single-value messaging
* **EventBased**: Event notification patterns
* **WaitSetMultiplexing**: Efficient event monitoring
