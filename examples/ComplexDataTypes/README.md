# Complex Data Types Example

This example demonstrates how to use complex data types (structs) with iceoryx2 in
C#. It shows how to define custom structs that can be sent and received across
process boundaries using zero-copy shared memory communication.

## Features

This example demonstrates:

1. **Simple Structs**: `TransmissionData` with primitive fields
2. **Sensor Data**: More realistic struct with timestamps and sensor readings
3. **Fixed Arrays**: `Point3D` with embedded fixed-size arrays
4. **Cross-Language Compatibility**: Using `[Iox2Type]` attribute for type name mapping
5. **Memory Layout Control**: Using `[StructLayout(LayoutKind.Sequential)]`
   for C compatibility

## Key Concepts

### Memory Layout

For structs to work correctly across language boundaries, they must have a
predictable memory layout:

```csharp
[StructLayout(LayoutKind.Sequential)]  // Ensures fields are laid out in order
public struct TransmissionData
{
    public int X;      // 4 bytes
    public int Y;      // 4 bytes
    public double Funky;  // 8 bytes
    // Total: 16 bytes (plus any padding)
}
```

### Type Name Mapping

The `[Iox2Type]` attribute allows you to specify the type name used for cross-
language communication:

```csharp
[Iox2Type("TransmissionData")]  // Must match the name used in Rust/C
public struct TransmissionData
{
    // ...
}
```

Without this attribute, the C# type name (e.g., "TransmissionData") is used by default.

### Unmanaged Constraint

All types used with iceoryx2 must be `unmanaged`, meaning they:

* Cannot contain reference types (classes, strings, etc.)
* Cannot contain managed pointers
* Can only contain other unmanaged types
* Can be safely copied byte-by-byte

```csharp
// ✓ Valid unmanaged types
public struct SensorData
{
    public long Timestamp;
    public float Temperature;
    public float Humidity;
    public int SensorId;
}

// ✗ Invalid - contains managed type (string)
public struct InvalidData
{
    public int Id;
    public string Name;  // ERROR: string is a managed type
}
```

## How to Build

```bash
cd /path/to/iceoryx2/iceoryx2-ffi/csharp
dotnet build examples/ComplexDataTypes/ComplexDataTypes.csproj
```

## How to Run

### Terminal 1 - Publisher

```bash
cd examples/ComplexDataTypes
dotnet run -- publisher transmission
```

Or with other data types:

```bash
dotnet run -- publisher sensor
dotnet run -- publisher point
```

### Terminal 2 - Subscriber

```bash
cd examples/ComplexDataTypes
dotnet run -- subscriber transmission
```

Or matching the publisher's data type:

```bash
dotnet run -- subscriber sensor
dotnet run -- subscriber point
```

## Example Output

### Publisher

```text
[Publisher] Starting with type: TransmissionData
[Publisher] Type size: 16 bytes
[Publisher] Service: ComplexTypes/Transmission
Publisher created successfully. Press Ctrl+C to stop.
Sending: TransmissionData { x: 0, y: 0, funky: 0.00 }
Sending: TransmissionData { x: 1, y: 3, funky: 812.12 }
Sending: TransmissionData { x: 2, y: 6, funky: 1624.24 }
...
```

### Subscriber

```text
[Subscriber] Starting with type: TransmissionData
[Subscriber] Type size: 16 bytes
[Subscriber] Service: ComplexTypes/Transmission
Subscriber ready. Waiting for samples... Press Ctrl+C to stop.
Received: TransmissionData { x: 0, y: 0, funky: 0.00 }
Received: TransmissionData { x: 1, y: 3, funky: 812.12 }
Received: TransmissionData { x: 2, y: 6, funky: 1624.24 }
...
```

## Cross-Language Communication

To communicate with Rust or C applications, ensure:

1. **Type names match**: Use `[Iox2Type("YourTypeName")]` or ensure C# type
   name matches
2. **Memory layout matches**: Use `[StructLayout(LayoutKind.Sequential)]` and
   matching field types
3. **Size and alignment match**: Verify with `sizeof()` in both languages
4. **Service names match**: Use the same service name string

### Example: Rust ↔ C Sharp

**Rust (rust_publisher.rs):**

```rust
#[repr(C)]
struct TransmissionData {
    x: i32,
    y: i32,
    funky: f64,
}
```

**C# (csharp_subscriber.cs):**

```csharp
[StructLayout(LayoutKind.Sequential)]
[Iox2Type("TransmissionData")]
public struct TransmissionData
{
    public int X;      // i32 in Rust
    public int Y;      // i32 in Rust
    public double Funky;  // f64 in Rust
}
```

Both use service name: `"ComplexTypes/Transmission"`

## Best Practices

1. **Always use `[StructLayout(LayoutKind.Sequential)]`** for interop structs
2. **Document field sizes** and total struct size for clarity
3. **Use `[Iox2Type]`** when communicating with other languages
4. **Test cross-language** communication thoroughly
5. **Avoid padding issues** by ordering fields from largest to smallest
6. **Keep structs simple** - avoid nested structs unless necessary
7. **Use fixed-size arrays** (`fixed`) sparingly, as they require `unsafe` code

## Troubleshooting

### Type Size Mismatch

If you get errors about type size mismatches:

* Verify struct size in both languages using `sizeof()`
* Check for padding differences (use `#pragma pack` in C or `Pack` in C#)
* Ensure field types match (e.g., `int32_t` in C = `int` in C#)

### Type Name Not Found

If the subscriber can't find the service:

* Ensure `[Iox2Type]` names match exactly
* Verify service names match (case-sensitive)
* Check that publisher started before subscriber

### Memory Corruption

If you get crashes or corrupted data:

* Ensure `[StructLayout(LayoutKind.Sequential)]` is present
* Verify alignment requirements match
* Check for C# auto-properties (use fields instead)
* Ensure no managed types sneaked into the struct
