# Blackboard Example

This example demonstrates the **blackboard pattern** in iceoryx2, which provides a shared key-value store for inter-process communication. Multiple processes can read and write data to specific keys in the blackboard, making it ideal for scenarios where multiple components need to access the latest state of various data points.

## Use Case

The example simulates a sensor monitoring system with three sensors:
- **Temperature** sensor (20-30°C)
- **Humidity** sensor (40-80%)
- **Pressure** sensor (1010-1030 hPa)

Each sensor reading includes a value, timestamp, and quality indicator.

## How It Works

The blackboard pattern allows:
- One **creator** process that initializes the blackboard with sensor entries and continuously updates them
- Multiple **opener** processes that can read the current sensor values independently

Unlike publish-subscribe, the blackboard provides access to the **latest value** rather than a stream of all updates. This makes it efficient for state monitoring where only the current reading matters.

## Running the Example

### Terminal 1 - Start the Creator
```bash
dotnet run -- creator
```

The creator will:
1. Create a blackboard service named "Blackboard/Sensors"
2. Initialize three sensor entries (Temperature, Humidity, Pressure)
3. Update sensor values every second with random data

### Terminal 2 - Start the Opener
```bash
dotnet run -- opener
```

The opener will:
1. Connect to the existing blackboard service
2. Read all sensor values every 500ms
3. Display the current readings

You can run multiple openers simultaneously to see independent access to the same data.

## Key Concepts

### Blackboard Entry
Each entry consists of a key-value pair:
- **Key**: `SensorKey` enum (Temperature, Humidity, Pressure)
- **Value**: `SensorData` struct containing measurement data

### Writer Operations
```csharp
var entry = writer.Entry<SensorData>(SensorKey.Temperature);
entry.Update(newData);
```

### Reader Operations
```csharp
var entry = reader.Entry<SensorData>(SensorKey.Temperature);
var data = entry.Get();
```

## Comparison with Pub-Sub

| Blackboard | Publish-Subscribe |
|------------|-------------------|
| Latest value only | All messages |
| Multiple independent keys | Single message type |
| Direct key access | Sequential message stream |
| Ideal for state monitoring | Ideal for event streams |

## Stopping the Example

Press `Ctrl+C` in either terminal to stop the respective process. The blackboard service will remain available as long as the creator is running.
