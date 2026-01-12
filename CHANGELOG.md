# Changelog

All notable changes to the iceoryx2-csharp bindings will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Features

<!-- New features go here -->

### Bugfixes

<!-- Bug fixes go here -->

### Refactoring

<!-- Code refactoring, internal improvements go here -->

### API Breaking Changes

<!-- Breaking changes that require user action go here -->

---

<!--

## [0.1.0] - Initial Release

Based on iceoryx2 v0.8.0

### Features

- **Core Bindings**
  - P/Invoke bindings to iceoryx2 native library (C FFI)
  - Support for .NET 8.0 and .NET 9.0
  - Cross-platform support (Windows, Linux, macOS)

- **Publish-Subscribe Pattern**
  - `Publisher<T>` and `Subscriber<T>` for typed messaging
  - Zero-copy message transfer via shared memory
  - Support for dynamic payloads

- **Request-Response Pattern**
  - `Client<TRequest, TResponse>` and `Server<TRequest, TResponse>` support
  - Typed request/response communication

- **Event System**
  - `Notifier` and `Listener` for event-based signaling
  - `WaitSet` for multiplexed event handling
  - `IAsyncEnumerable<T>` support for async event consumption

- **Service Discovery**
  - Service discovery APIs for runtime service enumeration

- **Reactive Extensions** (`Iceoryx2.Reactive`)
  - `IObservable<T>` integration for reactive programming
  - `ObservableWaitSet` for reactive event handling

- **Logging Integration**
  - `Microsoft.Extensions.Logging` integration
  - Configurable log levels

- **Quality of Service**
  - Configurable buffer sizes
  - History and subscriber settings

### Examples

- PublishSubscribe - Basic pub/sub pattern
- AsyncPubSub - Async/await pub/sub usage
- Event - Event notification example
- WaitSetMultiplexing - Multiplexed event handling
- WaitSetAsyncEnumerable - Async enumerable events
- ReactiveExample - Reactive extensions usage
- ObservableWaitSet - Observable pattern with WaitSet
- RequestResponse - Request/response pattern
- ServiceDiscovery - Service enumeration
- ComplexDataTypes - Structured data transfer
- Logging - Logging configuration
- QualityOfService - QoS settings

---

[Unreleased]: https://github.com/eclipse-iceoryx2/iceoryx2-csharp/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/eclipse-iceoryx2/iceoryx2-csharp/releases/tag/v0.1.0

-->
