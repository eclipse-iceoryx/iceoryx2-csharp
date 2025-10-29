# Rich Error Handling in iceoryx2 C# Bindings

The iceoryx2 C# bindings provide rich, contextual error information through a class hierarchy that enables better diagnostics and troubleshooting.

## Overview

Instead of simple enum values, errors are now represented as rich error objects that carry contextual information:

```csharp
// Base class for all iceoryx2 errors
public abstract class Iox2Error
{
    public abstract string Message { get; }
    public abstract Iox2ErrorKind Kind { get; }
    public virtual string? Details { get; }
}
```

## Error Hierarchy

### Service Errors

**ServiceCreationError** - Includes service name and detailed failure reason:

```csharp
var result = node.ServiceBuilder()
    .PublishSubscribe<MyData>()
    .Create("my_service");

if (result.IsErr())
{
    var error = result.Error;
    // error is ServiceCreationError with:
    // - ServiceName: "my_service"
    // - Details: specific failure reason
    // - Message: "Failed to create service 'my_service'. Details: <reason>"
    
    Console.WriteLine(error.Message);
    
    // Pattern matching on error type
    if (error is ServiceCreationError serviceError)
    {
        Console.WriteLine($"Service: {serviceError.ServiceName}");
        Console.WriteLine($"Details: {serviceError.Details}");
    }
}
```

### Event Errors

**NotifyError** - Includes event ID that failed:

```csharp
var result = notifier.Notify(eventId);

if (result.IsErr())
{
    var error = result.Error;
    if (error is NotifyError notifyError && notifyError.EventId.HasValue)
    {
        Console.WriteLine($"Failed to notify event {notifyError.EventId.Value}");
    }
}
```

### Handle Errors

**InvalidHandleError** - Includes handle type information:

```csharp
var error = new InvalidHandleError("Publisher", "Handle was disposed");
// Message: "Invalid Publisher handle. Details: Handle was disposed"
```

## Pattern Matching

Use pattern matching to handle specific error types:

```csharp
var result = DoSomething();

if (result.IsErr())
{
    switch (result.Error)
    {
        case ServiceCreationError serviceError:
            Logger.LogError("Failed to create service '{ServiceName}': {Details}",
                serviceError.ServiceName, serviceError.Details);
            break;
            
        case PublisherCreationError publisherError:
            Logger.LogError("Failed to create publisher: {Details}",
                publisherError.Details);
            break;
            
        case SampleLoanError loanError:
            Logger.LogWarning("Out of memory: {Details}", loanError.Details);
            break;
            
        default:
            Logger.LogError("Unknown error: {Message}", result.Error.Message);
            break;
    }
}
```

## Error Kinds for Compatibility

For backward compatibility and simple pattern matching, use `Iox2ErrorKind`:

```csharp
if (result.IsErr() && result.Error.Kind == Iox2ErrorKind.ServiceCreationFailed)
{
    // Handle service creation failure
}
```

## Creating Errors with Context

When creating errors programmatically, provide context:

```csharp
// With service name
var error = new ServiceCreationError("my_service", "Permission denied");

// With event ID
var error = new NotifyError(eventId, "No listeners attached");

// With handle type
var error = new InvalidHandleError("Subscriber", "Handle already disposed");
```

## Structured Logging Integration

The rich error objects work seamlessly with structured logging:

```csharp
var result = publisher.LoanUninit();

if (result.IsErr())
{
    logger.LogError("Sample loan failed: {@Error}", result.Error);
    // Logs: {
    //   "Kind": "SampleLoanFailed",
    //   "Message": "Failed to loan sample. Details: Memory exhausted",
    //   "Details": "Memory exhausted"
    // }
}
```

## All Error Types

### Node Errors

* `NodeCreationError` - Node creation failed

### Service Errors

* `ServiceCreationError` - Publish/subscribe service creation failed
* `EventServiceCreationError` - Event service creation failed
* `RequestResponseServiceCreationError` - Request/response service creation failed

### Publisher/Subscriber Errors

* `PublisherCreationError` - Publisher creation failed
* `SubscriberCreationError` - Subscriber creation failed
* `SampleLoanError` - Failed to loan a sample
* `SendError` - Failed to send data
* `ReceiveError` - Failed to receive data

### Event Errors

* `NotifierCreationError` - Notifier creation failed
* `ListenerCreationError` - Listener creation failed
* `NotifyError` - Failed to notify event
* `WaitError` - Failed to wait for event

### Request/Response Errors

* `ClientCreationError` - Client creation failed
* `ServerCreationError` - Server creation failed
* `RequestLoanError` - Failed to loan a request
* `RequestSendError` - Failed to send request
* `ResponseLoanError` - Failed to loan a response
* `ResponseSendError` - Failed to send response
* `ResponseReceiveError` - Failed to receive response

### WaitSet Errors

* `WaitSetCreationError` - WaitSet creation failed
* `WaitSetAttachmentError` - Failed to attach to WaitSet
* `WaitSetRunError` - WaitSet run operation failed

### General Errors

* `InvalidHandleError` - Invalid handle usage
* `UnknownError` - Unknown or unclassified error

## Benefits

1. **Rich Context** - Error messages include relevant information like service names, event IDs, handle types
2. **Type Safety** - Pattern matching on error types at compile time
3. **Structured Logging** - Error objects serialize well for logging frameworks
4. **Debugging** - Clear, actionable error messages help identify root causes quickly
5. **Backward Compatible** - Existing code using `Iox2Error.ServiceCreationFailed` still works
