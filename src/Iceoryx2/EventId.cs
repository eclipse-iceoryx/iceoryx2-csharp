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

using System;

namespace Iceoryx2;

/// <summary>
/// Represents an event ID used in the event messaging pattern.
/// Event IDs are simple numeric identifiers that distinguish different types of events.
/// </summary>
public readonly struct EventId : IEquatable<EventId>
{
    private readonly ulong _value;

    /// <summary>
    /// Creates a new EventId with the specified value.
    /// </summary>
    /// <param name="value">The numeric value of the event ID.</param>
    public EventId(ulong value)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the numeric value of this event ID.
    /// </summary>
    public ulong Value => _value;

    /// <summary>
    /// Converts the EventId to its native representation.
    /// </summary>
    internal Native.Iox2NativeMethods.iox2_event_id_t ToNative()
    {
        return new Native.Iox2NativeMethods.iox2_event_id_t { value = (UIntPtr)_value };
    }

    /// <summary>
    /// Creates an EventId from its native representation.
    /// </summary>
    internal static EventId FromNative(Native.Iox2NativeMethods.iox2_event_id_t native)
    {
        return new EventId((ulong)native.value);
    }

    /// <inheritdoc/>
    public bool Equals(EventId other) => _value == other._value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is EventId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => _value.GetHashCode();

    /// <inheritdoc/>
    public override string ToString() => _value.ToString();

    /// <summary>
    /// Determines whether two EventId instances are equal.
    /// </summary>
    public static bool operator ==(EventId left, EventId right) => left.Equals(right);

    /// <summary>
    /// Determines whether two EventId instances are not equal.
    /// </summary>
    public static bool operator !=(EventId left, EventId right) => !left.Equals(right);

    /// <summary>
    /// Implicitly converts an EventId to a ulong value.
    /// </summary>
    public static implicit operator ulong(EventId eventId) => eventId._value;

    /// <summary>
    /// Implicitly converts a ulong value to an EventId.
    /// </summary>
    public static implicit operator EventId(ulong value) => new EventId(value);
}