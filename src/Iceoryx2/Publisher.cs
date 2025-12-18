// Copyright (c) 2025 Contributors to the Eclipse Foundation
//
// See the NOTICE file(s) distributed with this work for additional
// information regarding copyright ownership.
//
// This program and the accompanying materials are made available under the
// terms of the Apache Software License 2.0 which is available at
// https://www.apache.org/licenses/LICENSE-2.0, or the MIT license
// which is available at https://opensource.org/licenses/MIT.
//
// SPDX-License-Identifier: Apache-2.0 OR MIT

using Iceoryx2.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace Iceoryx2;

/// <summary>
/// Delegate for initializing a payload by reference.
/// </summary>
/// <typeparam name="T">The payload type</typeparam>
/// <param name="payload">Reference to the payload to initialize</param>
public delegate void PayloadInitializer<T>(ref T payload) where T : unmanaged;

/// <summary>
/// A publisher that can send data samples to subscribers.
/// </summary>
public sealed class Publisher : IDisposable
{
    private SafePublisherHandle _handle;
    private bool _disposed;

    internal Publisher(SafePublisherHandle handle)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
    }

    /// <summary>
    /// Loans a sample for sending data.
    /// </summary>
    public Result<Sample<T>, Iox2Error> Loan<T>() where T : unmanaged
    {
        ThrowIfDisposed();

        try
        {
            // Loan sample - pass by reference for publisher handle
            var publisherHandle = _handle.DangerousGetHandle();
            var result = Native.Iox2NativeMethods.iox2_publisher_loan_slice_uninit(
                ref publisherHandle,  // Pass by reference - C expects pointer to handle
                IntPtr.Zero,  // NULL - let C allocate the struct
                out var sampleHandle,
                (UIntPtr)1);  // size_t in C = UIntPtr in C#

            if (result != Native.Iox2NativeMethods.IOX2_OK || sampleHandle == IntPtr.Zero)
                return Result<Sample<T>, Iox2Error>.Err(Iox2Error.SampleLoanFailed);

            var handle = new SafeSampleHandle(sampleHandle, isMutable: true);
            var sample = new Sample<T>(handle);

            return Result<Sample<T>, Iox2Error>.Ok(sample);
        }
        catch (Exception)
        {
            return Result<Sample<T>, Iox2Error>.Err(Iox2Error.SampleLoanFailed);
        }
    }

    /// <summary>
    /// Send a copy of the provided managed struct via the native send-copy path.
    /// This is a fallback that avoids the loan/send lifecycle and is useful for complex types.
    /// </summary>
    public Result<Unit, Iox2Error> SendCopy<T>(T value) where T : unmanaged
    {
        ThrowIfDisposed();

        try
        {
            var publisherHandle = _handle.DangerousGetHandle();
            var size = (ulong)Marshal.SizeOf<T>();
            var tmp = Marshal.AllocHGlobal((int)size);
            try
            {
                Marshal.StructureToPtr(value, tmp, false);
                var result = Native.Iox2NativeMethods.iox2_publisher_send_copy(
                    ref publisherHandle,
                    tmp,
                    (UIntPtr)size,
                    IntPtr.Zero);

                if (result != Native.Iox2NativeMethods.IOX2_OK)
                    return Result<Unit, Iox2Error>.Err(Iox2Error.SendFailed);

                return Result<Unit, Iox2Error>.Ok(Unit.Value);
            }
            finally
            {
                Marshal.FreeHGlobal(tmp);
            }
        }
        catch (Exception)
        {
            return Result<Unit, Iox2Error>.Err(Iox2Error.SendFailed);
        }
    }

    /// <summary>
    /// Convenience method: Loans a sample, writes the value to it, and sends it in one operation.
    /// This combines Loan() + Sample.Payload = value + Sample.Send() for simplicity.
    /// </summary>
    /// <param name="value">The value to send</param>
    /// <returns>Result indicating success or error</returns>
    /// <remarks>
    /// This is a convenience overload for common use cases. For more control over the
    /// loan/write/send lifecycle (e.g., writing complex data structures incrementally),
    /// use the explicit Loan() method followed by Sample operations.
    /// </remarks>
    public Result<Unit, Iox2Error> Send<T>(T value) where T : unmanaged
    {
        ThrowIfDisposed();

        var loanResult = Loan<T>();
        if (!loanResult.IsOk)
            return loanResult.Match(
                _ => Result<Unit, Iox2Error>.Ok(Unit.Value), // Won't happen
                err => Result<Unit, Iox2Error>.Err(err));

        using var sample = loanResult.Unwrap();
        sample.Payload = value;
        return sample.Send();
    }

    /// <summary>
    /// Convenience method: Loans a sample, allows initialization via callback, and sends it.
    /// This is useful when you want to write complex data or perform custom initialization.
    /// </summary>
    /// <param name="initializer">A callback that initializes the sample payload by reference</param>
    /// <returns>Result indicating success or error</returns>
    /// <remarks>
    /// This overload provides more flexibility than Send(T value) while still maintaining
    /// a simple single-call API. The initializer receives a reference to the payload
    /// that can be modified in-place.
    /// 
    /// Example:
    /// <code>
    /// publisher.SendWith&lt;MyData&gt;((ref MyData payload) => {
    ///     payload.field1 = 42;
    ///     payload.field2 = 123;
    /// });
    /// </code>
    /// </remarks>
    public Result<Unit, Iox2Error> SendWith<T>(PayloadInitializer<T> initializer) where T : unmanaged
    {
        ThrowIfDisposed();

        if (initializer == null)
            throw new ArgumentNullException(nameof(initializer));

        var loanResult = Loan<T>();
        if (!loanResult.IsOk)
            return loanResult.Match(
                _ => Result<Unit, Iox2Error>.Ok(Unit.Value), // Won't happen
                err => Result<Unit, Iox2Error>.Err(err));

        using var sample = loanResult.Unwrap();

        // Read current payload, modify it, write it back
        var payload = sample.Payload;
        initializer(ref payload);
        sample.Payload = payload;

        return sample.Send();
    }

    /// <summary>
    /// Convenience method: Loans a sample, uses a function to create the value, and sends it.
    /// This is useful when the value creation is expensive and should only happen if loan succeeds.
    /// </summary>
    /// <param name="factory">A function that creates the value to send</param>
    /// <returns>Result indicating success or error</returns>
    /// <remarks>
    /// The factory function is only called if the loan succeeds, which can be useful
    /// for lazy initialization or when value creation is expensive.
    /// 
    /// Example:
    /// <code>
    /// publisher.SendLazy(() => new MyData { 
    ///     field1 = ExpensiveComputation(),
    ///     field2 = AnotherExpensiveOperation()
    /// });
    /// </code>
    /// </remarks>
    public Result<Unit, Iox2Error> SendLazy<T>(Func<T> factory) where T : unmanaged
    {
        ThrowIfDisposed();

        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        var loanResult = Loan<T>();
        if (!loanResult.IsOk)
            return loanResult.Match(
                _ => Result<Unit, Iox2Error>.Ok(Unit.Value), // Won't happen
                err => Result<Unit, Iox2Error>.Err(err));

        using var sample = loanResult.Unwrap();
        var value = factory();
        sample.Payload = value;
        return sample.Send();
    }

    /// <summary>
    /// Explicitly updates all connections to the Subscribers. This is
    /// required to be called whenever a new Subscriber is connected to
    /// the service. It is called implicitly whenever Sample.Send() or
    /// Publisher.SendCopy() is called.
    /// When a Subscriber is connected that requires a history this
    /// call will deliver it.
    /// </summary>
    /// <returns>Result indicating success or connection failure</returns>
    /// <remarks>
    /// This method is critical for history delivery. When a service is configured
    /// with a history size and a new subscriber connects, the publisher must
    /// explicitly call UpdateConnections() to deliver the historical samples
    /// to the newly connected subscriber.
    /// 
    /// Example:
    /// <code>
    /// // Service configured with history
    /// var service = node.ServiceBuilder(serviceName)
    ///     .PublishSubscribe&lt;ulong&gt;()
    ///     .HistorySize(5)
    ///     .Open();
    /// 
    /// var publisher = service.PublisherBuilder().Create();
    /// 
    /// // Send some samples
    /// for (ulong i = 1; i &lt;= 5; i++) {
    ///     publisher.Send(i);
    /// }
    /// 
    /// // Late-joining subscriber
    /// var subscriber = service.SubscriberBuilder()
    ///     .BufferSize(10)
    ///     .Create();
    /// 
    /// // REQUIRED: Update connections to deliver history
    /// publisher.UpdateConnections();
    /// 
    /// // Now subscriber can receive the 5 historical samples
    /// while (var sample = subscriber.Receive()) {
    ///     Console.WriteLine($"History: {sample.Payload}");
    /// }
    /// </code>
    /// </remarks>
    public Result<Unit, Iox2Error> UpdateConnections()
    {
        ThrowIfDisposed();

        try
        {
            var publisherHandle = _handle.DangerousGetHandle();
            var result = Native.Iox2NativeMethods.iox2_publisher_update_connections(ref publisherHandle);

            if (result != Native.Iox2NativeMethods.IOX2_OK)
                return Result<Unit, Iox2Error>.Err(Iox2Error.ConnectionUpdateFailed);

            return Result<Unit, Iox2Error>.Ok(Unit.Value);
        }
        catch (Exception)
        {
            return Result<Unit, Iox2Error>.Err(Iox2Error.ConnectionUpdateFailed);
        }
    }

    /// <summary>
    /// Disposes of the resources used by the Publisher instance.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _handle?.Dispose();
            _disposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Publisher));
    }
}