using Iceoryx2;
using System;
using System.Runtime.InteropServices;

namespace ArrayOfStructsExample;

/// <summary>
/// Simple struct to be used in arrays.
/// Must use StructLayout(LayoutKind.Sequential) for memory layout compatibility.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[Iox2Type("Particle")]
public struct Particle
{
    public float X;
    public float Y;
    public float Z;
    public float Velocity;
    public int Id;

    public Particle(float x, float y, float z, float velocity, int id)
    {
        X = x;
        Y = y;
        Z = z;
        Velocity = velocity;
        Id = id;
    }

    public override string ToString()
    {
        return $"Particle[{Id}] {{ pos: ({X:F2}, {Y:F2}, {Z:F2}), vel: {Velocity:F2} }}";
    }
}

/// <summary>
/// Struct representing sensor reading with timestamp.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[Iox2Type("SensorReading")]
public struct SensorReading
{
    public long Timestamp;
    public float Temperature;
    public float Pressure;
    public float Humidity;
    public ushort SensorId;

    public SensorReading(long timestamp, float temperature, float pressure, float humidity, ushort sensorId)
    {
        Timestamp = timestamp;
        Temperature = temperature;
        Pressure = pressure;
        Humidity = humidity;
        SensorId = sensorId;
    }

    public override string ToString()
    {
        return $"Sensor[{SensorId}] @ {Timestamp}: Temp={Temperature:F1}°C, Press={Pressure:F1}hPa, Hum={Humidity:F1}%";
    }
}

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Iceoryx2 C# Array of Structs Example ===\n");
        Console.WriteLine("This example demonstrates how to send arrays containing structs");
        Console.WriteLine("using iceoryx2's zero-copy inter-process communication.\n");

        if (args.Length < 1)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run publisher [particle|sensor]");
            Console.WriteLine("  dotnet run subscriber [particle|sensor]");
            Console.WriteLine("\nExamples:");
            Console.WriteLine("  Terminal 1: dotnet run subscriber particle");
            Console.WriteLine("  Terminal 2: dotnet run publisher particle");
            return;
        }

        var mode = args[0].ToLower();
        var dataType = args.Length > 1 ? args[1].ToLower() : "particle";

        try
        {
            switch (mode)
            {
                case "publisher":
                    if (dataType == "sensor")
                        RunPublisher<SensorReading>("array-sensor-service", CreateSensorArray);
                    else
                        RunPublisher<Particle>("array-particle-service", CreateParticleArray);
                    break;

                case "subscriber":
                    if (dataType == "sensor")
                        RunSubscriber<SensorReading>("array-sensor-service");
                    else
                        RunSubscriber<Particle>("array-particle-service");
                    break;

                default:
                    Console.WriteLine($"Unknown mode: {mode}. Use 'publisher' or 'subscriber'");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error: {ex.Message}");
            Console.WriteLine($"Stack trace:\n{ex.StackTrace}");
        }
    }

    static unsafe void RunPublisher<T>(string serviceName, Func<int, T[]> createArrayFunc) where T : unmanaged
    {
        Console.WriteLine($"📤 [Publisher] Type: {typeof(T).Name}");
        Console.WriteLine($"   Service: {serviceName}");
        Console.WriteLine($"   Struct size: {sizeof(T)} bytes\n");

        // Create a node
        using var node = NodeBuilder.New()
            .Name("array-publisher-node")
            .Create()
            .Expect("Failed to create node");

        // Open or create the service for arrays of T
        // The service will handle slices/arrays of the struct type
        using var service = node.ServiceBuilder()
            .PublishSubscribe<T>()
            .Open(serviceName)
            .Expect($"Failed to open service '{serviceName}'");

        // Create a publisher
        using var publisher = service.CreatePublisher()
            .Expect("Failed to create publisher");

        Console.WriteLine("✅ Publisher created successfully.");
        Console.WriteLine("Press Ctrl+C to stop.\n");

        var iteration = 0;
        while (true)
        {
            // Generate an array of structs
            var arraySize = 5 + (iteration % 6); // Vary array size from 5 to 10
            var dataArray = createArrayFunc(iteration);

            Console.WriteLine($"📦 Iteration {iteration}: Sending array of {dataArray.Length} {typeof(T).Name} structs");

            // Loan a slice (array) - this allocates shared memory for the entire array
            var sample = publisher.LoanSlice((ulong)dataArray.Length)
                .Expect("Failed to loan slice");

            try
            {
                // Get the payload as a span and copy our data into it
                var payload = sample.Payload;
                
                // Copy the array into the loaned slice
                for (int i = 0; i < dataArray.Length && i < payload.Length; i++)
                {
                    payload[i] = dataArray[i];
                    Console.WriteLine($"   [{i}] {dataArray[i]}");
                }

                // Send the sample with the array
                sample.Send()
                    .Expect("Failed to send sample");

                Console.WriteLine($"✅ Sent successfully!\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during send: {ex.Message}");
                // Sample will be disposed automatically
            }

            iteration++;
            System.Threading.Thread.Sleep(2000); // Send every 2 seconds
        }
    }

    static unsafe void RunSubscriber<T>(string serviceName) where T : unmanaged
    {
        Console.WriteLine($"📥 [Subscriber] Type: {typeof(T).Name}");
        Console.WriteLine($"   Service: {serviceName}");
        Console.WriteLine($"   Struct size: {sizeof(T)} bytes\n");

        // Create a node
        using var node = NodeBuilder.New()
            .Name("array-subscriber-node")
            .Create()
            .Expect("Failed to create node");

        // Open the service
        using var service = node.ServiceBuilder()
            .PublishSubscribe<T>()
            .Open(serviceName)
            .Expect($"Failed to open service '{serviceName}'");

        // Create a subscriber with a larger buffer to handle arrays
        using var subscriber = service.SubscriberBuilder()
            .Create()
            .Expect("Failed to create subscriber");

        Console.WriteLine("✅ Subscriber created successfully.");
        Console.WriteLine("Waiting for data...\n");

        var receivedCount = 0;
        while (true)
        {
            // Receive a sample (which may contain an array)
            var receiveResult = subscriber.Receive<T>();

            if (receiveResult.IsOk)
            {
                var sample = receiveResult.Unwrap();
                if (sample != null)
                {
                    using (sample)
                    {
                        var payload = sample.Payload;
                        
                        Console.WriteLine($"📬 Received array with {payload.Length} elements:");
                        
                        // Process each struct in the array
                        for (int i = 0; i < payload.Length; i++)
                        {
                            Console.WriteLine($"   [{i}] {payload[i]}");
                        }
                        
                        receivedCount++;
                        Console.WriteLine($"✅ Total arrays received: {receivedCount}\n");
                    }
                }
                // else: no new sample available (normal condition)
            }
            else
            {
                var error = receiveResult.UnwrapErr();
                Console.WriteLine($"❌ Error receiving: {error}");
                break;
            }

            System.Threading.Thread.Sleep(100); // Poll every 100ms
        }
    }

    // Helper function to create particle arrays
    static Particle[] CreateParticleArray(int iteration)
    {
        var arraySize = 5 + (iteration % 6);
        var particles = new Particle[arraySize];
        
        var random = new Random(iteration); // Deterministic for demo
        
        for (int i = 0; i < arraySize; i++)
        {
            particles[i] = new Particle(
                x: (float)(random.NextDouble() * 100),
                y: (float)(random.NextDouble() * 100),
                z: (float)(random.NextDouble() * 100),
                velocity: (float)(random.NextDouble() * 10),
                id: iteration * 100 + i
            );
        }
        
        return particles;
    }

    // Helper function to create sensor reading arrays
    static SensorReading[] CreateSensorArray(int iteration)
    {
        var arraySize = 5 + (iteration % 6);
        var readings = new SensorReading[arraySize];
        
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = new Random(iteration);
        
        for (int i = 0; i < arraySize; i++)
        {
            readings[i] = new SensorReading(
                timestamp: timestamp + i * 100, // 100ms apart
                temperature: 20.0f + (float)(random.NextDouble() * 10),
                pressure: 1000.0f + (float)(random.NextDouble() * 50),
                humidity: 40.0f + (float)(random.NextDouble() * 20),
                sensorId: (ushort)(100 + i)
            );
        }
        
        return readings;
    }
}
