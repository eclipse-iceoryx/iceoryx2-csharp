# Architecture

This document explains the architecture of iceoryx2 and its C# bindings.

## Overview

iceoryx2 is a **zero-copy inter-process communication (IPC)** library that
enables high-performance data sharing between processes without serialization
or data copying. The C# bindings (`iceoryx2-csharp`) provide idiomatic .NET
access to iceoryx2 through a P/Invoke FFI layer.

```text
┌──────────────────────────────────────────────────────────────────────────┐
│                          Your C# Application                              │
├──────────────────────────────────────────────────────────────────────────┤
│                     iceoryx2-csharp (C# Bindings)                         │
│  ┌─────────────┐  ┌─────────────────┐  ┌─────────────────────────────┐   │
│  │ High-Level  │  │   SafeHandle    │  │     P/Invoke FFI Layer      │   │
│  │    APIs     │  │   Management    │  │  (Native Method Bindings)   │   │
│  └─────────────┘  └─────────────────┘  └─────────────────────────────┘   │
├──────────────────────────────────────────────────────────────────────────┤
│                      iceoryx2-ffi-c (C API)                               │
├──────────────────────────────────────────────────────────────────────────┤
│                      iceoryx2 (Rust Core)                                 │
├──────────────────────────────────────────────────────────────────────────┤
│                    Operating System (Shared Memory)                       │
└──────────────────────────────────────────────────────────────────────────┘
```

## Core Concepts

### Nodes

A **Node** is the entry point for all iceoryx2 operations. It represents your
application's identity and manages the lifecycle of services.

```csharp
using var node = NodeBuilder.New()
    .Name("my_application")
    .Create()
    .Unwrap();
```

Nodes provide:

* **Identity**: Unique name for identification in the system
* **Service Factory**: Creates and opens services
* **Monitoring**: Detect dead or unresponsive nodes
* **Cleanup**: Automatic resource cleanup when disposed

### Services

A **Service** defines a communication channel with a specific messaging pattern.
Services have unique names and are created/opened through a Node.

```csharp
var service = node.ServiceBuilder()
    .PublishSubscribe<MyData>()
    .Open("sensor_data")
    .Unwrap();
```

### Messaging Patterns

iceoryx2 supports three messaging patterns:

| Pattern | Endpoints | Data Flow | Use Case |
|---------|-----------|-----------|----------|
| **Publish-Subscribe** | Publishers → Subscribers | One-to-many | Sensor data, events |
| **Event** | Notifiers → Listeners | One-to-many (IDs only) | Wake-up signals |
| **Request-Response** | Clients ↔ Servers | Many-to-many | RPC, commands |

## Zero-Copy Communication

### How It Works

Traditional IPC copies data multiple times:

```text
Traditional IPC:
App A → Serialize → Kernel Buffer → User Buffer → Deserialize → App B
        (copy 1)      (copy 2)       (copy 3)       (copy 4)
```

iceoryx2 uses shared memory for true zero-copy:

```text
iceoryx2:
App A → Write to Shared Memory ← Read from Shared Memory ← App B
        (no copies - direct access)
```

### Data Layout Requirements

For zero-copy to work, both publisher and subscriber must interpret memory
identically. This requires:

1. **Sequential Layout**: `[StructLayout(LayoutKind.Sequential)]`
2. **Unmanaged Types**: No reference types (strings, arrays, classes)
3. **Fixed Size**: Size known at compile time

```csharp
// ✅ Valid for zero-copy
[StructLayout(LayoutKind.Sequential)]
public struct SensorReading
{
    public int SensorId;
    public double Temperature;
    public double Humidity;
    public long TimestampNs;
}

// ❌ Invalid - contains reference type
public struct InvalidData
{
    public string Name;  // Reference type!
    public int[] Values; // Reference type!
}
```

### Cross-Language Compatibility

When communicating with Rust or C applications, ensure memory layout matches:

| C# | Rust | C |
|----|------|---|
| `[StructLayout(LayoutKind.Sequential)]` | `#[repr(C)]` | Default struct |
| `int` | `i32` | `int32_t` |
| `uint` | `u32` | `uint32_t` |
| `long` | `i64` | `int64_t` |
| `ulong` | `u64` | `uint64_t` |
| `float` | `f32` | `float` |
| `double` | `f64` | `double` |

## Memory Safety

### SafeHandle Pattern

All native resources are wrapped in `SafeHandle` types, ensuring:

* **Deterministic cleanup**: Resources released when disposed
* **Thread safety**: Safe even if Dispose is called from multiple threads
* **Leak prevention**: Finalizer ensures cleanup even without Dispose

```csharp
// Using statement ensures cleanup
using var publisher = service.CreatePublisher().Unwrap();
// Publisher automatically disposed here
```

### Resource Hierarchy

Resources have dependencies that must be respected:

```text
Node (parent)
├── Service (child of Node)
│   ├── Publisher (child of Service)
│   ├── Subscriber (child of Service)
│   ├── Notifier (child of Service)
│   ├── Listener (child of Service)
│   ├── Client (child of Service)
│   └── Server (child of Service)
└── WaitSet (child of Node)
    └── Guards (children of WaitSet)
```

**Important**: Dispose children before parents. Using `using` statements
handles this automatically through scope.

## Error Handling

### Result Pattern

All fallible operations return `Result<T, Iox2Error>`:

```csharp
var result = node.ServiceBuilder()
    .Event()
    .Open("my_service");

if (result.IsOk)
{
    using var service = result.Unwrap();
    // Use service...
}
else
{
    Console.WriteLine($"Error: {result}");
}
```

### Common Methods

* `IsOk` - Check if operation succeeded
* `Unwrap()` - Get value (throws if error)
* `Expect(message)` - Get value with custom error message
* `UnwrapOr(default)` - Get value or default
* `Match(onOk, onError)` - Pattern matching

## Performance Considerations

### Loan vs Copy

iceoryx2 provides two ways to send data:

**Loan (Zero-Copy):**

```csharp
using var sample = publisher.Loan().Unwrap();
ref var data = ref sample.GetPayloadRef();
data.Value = 42;  // Write directly to shared memory
sample.Send();    // Transfer ownership, no copy
```

**Copy (Convenient):**

```csharp
publisher.SendCopy(new MyData { Value = 42 });  // Copies data
```

Use Loan for:

* Large payloads
* High-frequency updates
* Latency-sensitive applications

Use Copy for:

* Small payloads
* Simplicity over performance
* Infrequent updates

### WaitSet for Efficiency

Polling wastes CPU cycles. Use WaitSet for efficient event notification:

```csharp
// ❌ Inefficient polling
while (true)
{
    var sample = subscriber.Receive().Unwrap();
    if (sample != null) ProcessSample(sample);
    Thread.Sleep(1);  // Wasted CPU + latency
}

// ✅ Efficient WaitSet
using var waitset = WaitSetBuilder.New().Create().Unwrap();
using var guard = waitset.AttachNotification(listener).Unwrap();

waitset.WaitAndProcess((id) => {
    if (id.HasEventFrom(guard))
    {
        // Process events...
    }
    return CallbackProgression.Continue;
});
```

## Service Types

### Publish-Subscribe

Best for: Continuous data streams, sensor readings, telemetry

```text
┌───────────┐         ┌───────────┐
│ Publisher │────────▶│Subscriber1│
└───────────┘    │    └───────────┘
                 │    ┌───────────┐
                 └───▶│Subscriber2│
                      └───────────┘
```

### Event

Best for: Lightweight notifications, wake-up signals, state changes

```text
┌──────────┐  EventId  ┌──────────┐
│ Notifier │──────────▶│ Listener │
└──────────┘           └──────────┘
```

Events carry only an ID (ulong), not data payloads. Use when you need to
signal that "something happened" without transferring data.

### Request-Response

Best for: RPC, commands, queries

```text
┌────────┐  Request   ┌────────┐
│ Client │───────────▶│ Server │
│        │◀───────────│        │
└────────┘  Response  └────────┘
```

Clients send requests and wait for responses. Multiple clients can
connect to multiple servers.

## Domains

Domains provide **isolated communication groups**. Services in different
domains cannot see or communicate with each other.

Use cases:

* **Multi-tenant systems**: Isolate customer data
* **Test/Production separation**: Run both on same machine
* **Multiple instances**: Run same application multiple times

Domains are configured at the iceoryx2 system level, not through the C# API
directly. See the iceoryx2 documentation for domain configuration.

## Further Reading

* [iceoryx2 Rust Documentation](https://docs.rs/iceoryx2)
* [iceoryx2 GitHub Repository](https://github.com/eclipse-iceoryx/iceoryx2)
* [Examples](./examples/README.md)
