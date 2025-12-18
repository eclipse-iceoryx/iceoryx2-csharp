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
using System.Threading;
using System.Threading.Tasks;

namespace Iceoryx2;

/// <summary>
/// A subscriber that can receive data samples from publishers.
/// </summary>
public sealed class Subscriber : IDisposable
{
    private SafeSubscriberHandle _handle;
    private bool _disposed;

    internal Subscriber(SafeSubscriberHandle handle)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
    }

    /// <summary>
    /// Receives a sample if one is available.
    /// </summary>
    public Result<Sample<T>?, Iox2Error> Receive<T>() where T : unmanaged
    {
        ThrowIfDisposed();

        try
        {
            // Receive sample - pass by reference for subscriber handle
            var subscriberHandle = _handle.DangerousGetHandle();

            var result = Native.Iox2NativeMethods.iox2_subscriber_receive(
                ref subscriberHandle,  // Pass by reference - C expects pointer to handle
                IntPtr.Zero,  // NULL - let C allocate the struct
                out var sampleHandle);

            // No sample available is not an error
            if (result != Native.Iox2NativeMethods.IOX2_OK)
            {
                if (sampleHandle == IntPtr.Zero)
                    return Result<Sample<T>?, Iox2Error>.Ok(null);
                return Result<Sample<T>?, Iox2Error>.Err(Iox2Error.ReceiveFailed);
            }

            if (sampleHandle == IntPtr.Zero)
                return Result<Sample<T>?, Iox2Error>.Ok(null);

            var handle = new SafeSampleHandle(sampleHandle, isMutable: false);
            var sample = new Sample<T>(handle);

            return Result<Sample<T>?, Iox2Error>.Ok(sample);
        }
        catch (Exception)
        {
            return Result<Sample<T>?, Iox2Error>.Err(Iox2Error.ReceiveFailed);
        }
    }

    /// <summary>
    /// Asynchronously waits for a sample with a timeout by polling.
    /// </summary>
    /// <param name="timeout">The maximum time to wait for a sample.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the wait operation.</param>
    /// <returns>A Task containing a Result with the sample if received within timeout (null if timeout), or an error.</returns>
    public async Task<Result<Sample<T>?, Iox2Error>> ReceiveAsync<T>(TimeSpan timeout, CancellationToken cancellationToken = default) where T : unmanaged
    {
        ThrowIfDisposed();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = Receive<T>();
            if (!result.IsOk)
            {
                return result;
            }

            var sample = result.Unwrap();
            if (sample != null)
            {
                return Result<Sample<T>?, Iox2Error>.Ok(sample);
            }

            // Yield to thread pool instead of blocking
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
        }

        return Result<Sample<T>?, Iox2Error>.Ok(null);
    }

    /// <summary>
    /// Asynchronously waits for a sample indefinitely by polling.
    /// Note: This polls every 10ms since the native API doesn't have a blocking receive.
    /// The polling is efficient as it yields to the thread pool between checks.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token to cancel the wait operation.</param>
    /// <returns>A Task containing a Result with the sample or an error.</returns>
    public async Task<Result<Sample<T>, Iox2Error>> ReceiveAsync<T>(CancellationToken cancellationToken = default) where T : unmanaged
    {
        ThrowIfDisposed();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = Receive<T>();
            if (!result.IsOk)
            {
                return Result<Sample<T>, Iox2Error>.Err(Iox2Error.ReceiveFailed);
            }

            var sample = result.Unwrap();
            if (sample != null)
            {
                return Result<Sample<T>, Iox2Error>.Ok(sample);
            }

            // Yield to thread pool instead of blocking
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Convenience method: Receives a sample and immediately extracts its payload value.
    /// This combines Receive() + Sample.Payload access for simplicity.
    /// </summary>
    /// <returns>Result with the payload value if available, null if no sample, or an error</returns>
    /// <remarks>
    /// This is a convenience overload for cases where you just want the value without
    /// managing the Sample lifetime. The sample is automatically disposed after reading.
    /// For more control, use the explicit Receive() method.
    /// </remarks>
    public Result<T?, Iox2Error> TryReceiveValue<T>() where T : unmanaged
    {
        ThrowIfDisposed();

        var result = Receive<T>();
        if (!result.IsOk)
            return result.Match(
                _ => Result<T?, Iox2Error>.Ok(default(T?)), // Won't happen
                err => Result<T?, Iox2Error>.Err(err));

        using var sample = result.Unwrap();
        if (sample == null)
            return Result<T?, Iox2Error>.Ok(default(T?));

        return Result<T?, Iox2Error>.Ok(sample.Payload);
    }

    /// <summary>
    /// Convenience method: Receives a sample and processes it with a callback.
    /// The sample is automatically disposed after the callback completes.
    /// </summary>
    /// <param name="processor">Callback that processes the payload value</param>
    /// <returns>Result indicating whether processing succeeded, or an error</returns>
    /// <remarks>
    /// This is useful for processing samples without manually managing their lifetime.
    /// The processor is only called if a sample is available.
    /// 
    /// Example:
    /// <code>
    /// subscriber.ProcessSample&lt;MyData&gt;(data => {
    ///     Console.WriteLine($"Received: {data.value}");
    /// });
    /// </code>
    /// </remarks>
    public Result<bool, Iox2Error> ProcessSample<T>(Action<T> processor) where T : unmanaged
    {
        ThrowIfDisposed();

        if (processor == null)
            throw new ArgumentNullException(nameof(processor));

        var result = Receive<T>();
        if (!result.IsOk)
            return result.Match(
                _ => Result<bool, Iox2Error>.Ok(false), // Won't happen
                err => Result<bool, Iox2Error>.Err(err));

        using var sample = result.Unwrap();
        if (sample == null)
            return Result<bool, Iox2Error>.Ok(false); // No sample available

        processor(sample.Payload);
        return Result<bool, Iox2Error>.Ok(true); // Sample was processed
    }

    /// <summary>
    /// Convenience method: Asynchronously waits for and processes a sample with timeout.
    /// Combines ReceiveAsync() + processing in one operation.
    /// </summary>
    /// <param name="processor">Callback that processes the payload value</param>
    /// <param name="timeout">Maximum time to wait for a sample</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Result indicating whether a sample was received and processed</returns>
    public async Task<Result<bool, Iox2Error>> ProcessSampleAsync<T>(
        Action<T> processor,
        TimeSpan timeout,
        CancellationToken cancellationToken = default) where T : unmanaged
    {
        ThrowIfDisposed();

        if (processor == null)
            throw new ArgumentNullException(nameof(processor));

        var result = await ReceiveAsync<T>(timeout, cancellationToken);
        if (!result.IsOk)
            return result.Match(
                _ => Result<bool, Iox2Error>.Ok(false), // Won't happen
                err => Result<bool, Iox2Error>.Err(err));

        using var sample = result.Unwrap();
        if (sample == null)
            return Result<bool, Iox2Error>.Ok(false); // Timeout - no sample

        processor(sample.Payload);
        return Result<bool, Iox2Error>.Ok(true); // Sample was processed
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// Ensures proper cleanup by disposing of the associated resources when the object is no longer needed.
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
            throw new ObjectDisposedException(nameof(Subscriber));
    }
}