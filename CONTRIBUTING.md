# Contributing

Contributions are welcome! Here are some areas where you can help:

* 🧪 **Testing** - Add more unit tests and integration tests
* 📚 **Documentation** - Improve XML docs and add tutorials
* 🎯 **Examples** - Create examples for specific use cases
* 🐛 **Bug fixes** - Report and fix issues
* ✨ **New features** - Implement missing APIs (request-response, pipeline, etc.)

## Development Workflow

1. **Fork and clone** the repository
2. **Build the native library**: `cargo build --release --package iceoryx2-ffi-c`
3. **Build the C# bindings**: `cd iceoryx2-ffi/csharp && dotnet build`
4. **Run tests**: `dotnet test`
5. **Make your changes** and ensure tests pass
6. **Submit a pull request** with a clear description

## Code Style

* Follow standard C# conventions (PascalCase for public APIs)
* Add XML documentation comments to all public APIs
* Use `Result<T, E>` for fallible operations
* Implement `IDisposable` for resources that wrap native handles
* Use `SafeHandle` for all P/Invoke handles
