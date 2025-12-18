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

using Iceoryx2.Native;
using System;
using System.Text;

namespace Iceoryx2;

/// <summary>
/// Messaging pattern types supported by iceoryx2 services.
/// </summary>
public enum MessagingPattern
{
    /// <summary>
    /// Publish-Subscribe pattern for one-to-many communication.
    /// </summary>
    PublishSubscribe = 0,

    /// <summary>
    /// Event pattern for asynchronous notifications.
    /// </summary>
    Event = 1,

    /// <summary>
    /// Request-Response pattern for synchronous two-way communication.
    /// </summary>
    RequestResponse = 2,

    /// <summary>
    /// Blackboard pattern for shared state.
    /// </summary>
    Blackboard = 3
}

/// <summary>
/// Static configuration information for an iceoryx2 service.
/// Contains metadata and settings that define the service characteristics.
/// </summary>
public class ServiceStaticConfig
{
    /// <summary>
    /// Gets the unique identifier of the service.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the name of the service.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the messaging pattern used by this service.
    /// </summary>
    public MessagingPattern MessagingPattern { get; }

    /// <summary>
    /// Gets the event-specific configuration if the service uses the Event pattern.
    /// </summary>
    public EventStaticConfig? EventConfig { get; }

    /// <summary>
    /// Gets the publish-subscribe configuration if the service uses the PublishSubscribe pattern.
    /// </summary>
    public PublishSubscribeStaticConfig? PublishSubscribeConfig { get; }

    /// <summary>
    /// Gets the request-response configuration if the service uses the RequestResponse pattern.
    /// </summary>
    public RequestResponseStaticConfig? RequestResponseConfig { get; }

    /// <summary>
    /// Gets the blackboard configuration if the service uses the Blackboard pattern.
    /// </summary>
    public BlackboardStaticConfig? BlackboardConfig { get; }

    /// <summary>
    /// Internal constructor for creating a service config from basic parameters.
    /// Used when the full native struct cannot be safely marshaled.
    /// </summary>
    internal ServiceStaticConfig(byte[] id, byte[] name, Iox2NativeMethods.iox2_messaging_pattern_e messagingPattern)
    {
        Id = ExtractString(id);
        Name = ExtractString(name);
        MessagingPattern = (MessagingPattern)messagingPattern;
        // Pattern-specific configs are null when using this simplified constructor
    }

    internal ServiceStaticConfig(ref Iox2NativeMethods.iox2_static_config_t native)
    {
        // Extract ID (fixed-size byte array)
        Id = ExtractString(native.id);

        // Extract Name (fixed-size byte array)
        Name = ExtractString(native.name);

        // Map messaging pattern
        MessagingPattern = (MessagingPattern)native.messaging_pattern;

        // Extract pattern-specific configuration based on messaging pattern
        switch (MessagingPattern)
        {
            case MessagingPattern.Event:
                EventConfig = new EventStaticConfig(ref native.details.@event);
                break;
            case MessagingPattern.PublishSubscribe:
                PublishSubscribeConfig = new PublishSubscribeStaticConfig(ref native.details.publish_subscribe);
                break;
            case MessagingPattern.RequestResponse:
                RequestResponseConfig = new RequestResponseStaticConfig(ref native.details.request_response);
                break;
            case MessagingPattern.Blackboard:
                BlackboardConfig = new BlackboardStaticConfig(ref native.details.blackboard);
                break;
        }
    }

    private static string ExtractString(byte[] bytes)
    {
        // Find the null terminator
        int length = Array.IndexOf(bytes, (byte)0);
        if (length < 0)
        {
            length = bytes.Length;
        }

        return Encoding.UTF8.GetString(bytes, 0, length);
    }
}

/// <summary>
/// Static configuration for Event messaging pattern services.
/// </summary>
public class EventStaticConfig
{
    /// <summary>
    /// Gets the maximum number of notifiers.
    /// </summary>
    public ulong MaxNotifiers { get; }

    /// <summary>
    /// Gets the maximum number of listeners.
    /// </summary>
    public ulong MaxListeners { get; }

    /// <summary>
    /// Gets the maximum number of nodes.
    /// </summary>
    public ulong MaxNodes { get; }

    /// <summary>
    /// Gets the maximum event ID value.
    /// </summary>
    public ulong EventIdMaxValue { get; }

    /// <summary>
    /// Gets the notifier dead event ID if configured.
    /// </summary>
    public ulong? NotifierDeadEvent { get; }

    /// <summary>
    /// Gets the notifier dropped event ID if configured.
    /// </summary>
    public ulong? NotifierDroppedEvent { get; }

    /// <summary>
    /// Gets the notifier created event ID if configured.
    /// </summary>
    public ulong? NotifierCreatedEvent { get; }

    internal EventStaticConfig(ref Iox2NativeMethods.iox2_static_config_event_t native)
    {
        MaxNotifiers = (ulong)native.max_notifiers;
        MaxListeners = (ulong)native.max_listeners;
        MaxNodes = (ulong)native.max_nodes;
        EventIdMaxValue = (ulong)native.event_id_max_value;
        NotifierDeadEvent = native.has_notifier_dead_event ? (ulong)native.notifier_dead_event : null;
        NotifierDroppedEvent = native.has_notifier_dropped_event ? (ulong)native.notifier_dropped_event : null;
        NotifierCreatedEvent = native.has_notifier_created_event ? (ulong)native.notifier_created_event : null;
    }
}

/// <summary>
/// Static configuration for PublishSubscribe messaging pattern services.
/// </summary>
public class PublishSubscribeStaticConfig
{
    /// <summary>
    /// Gets the maximum number of subscribers.
    /// </summary>
    public ulong MaxSubscribers { get; }

    /// <summary>
    /// Gets the maximum number of publishers.
    /// </summary>
    public ulong MaxPublishers { get; }

    /// <summary>
    /// Gets the maximum number of nodes.
    /// </summary>
    public ulong MaxNodes { get; }

    /// <summary>
    /// Gets the history size.
    /// </summary>
    public ulong HistorySize { get; }

    /// <summary>
    /// Gets the maximum subscriber buffer size.
    /// </summary>
    public ulong SubscriberMaxBufferSize { get; }

    /// <summary>
    /// Gets the maximum number of borrowed samples per subscriber.
    /// </summary>
    public ulong SubscriberMaxBorrowedSamples { get; }

    /// <summary>
    /// Gets whether safe overflow is enabled.
    /// </summary>
    public bool EnableSafeOverflow { get; }

    internal PublishSubscribeStaticConfig(ref Iox2NativeMethods.iox2_static_config_publish_subscribe_t native)
    {
        MaxSubscribers = (ulong)native.max_subscribers;
        MaxPublishers = (ulong)native.max_publishers;
        MaxNodes = (ulong)native.max_nodes;
        HistorySize = (ulong)native.history_size;
        SubscriberMaxBufferSize = (ulong)native.subscriber_max_buffer_size;
        SubscriberMaxBorrowedSamples = (ulong)native.subscriber_max_borrowed_samples;
        EnableSafeOverflow = native.enable_safe_overflow;
    }
}

/// <summary>
/// Static configuration for RequestResponse messaging pattern services.
/// </summary>
public class RequestResponseStaticConfig
{
    /// <summary>
    /// Gets whether safe overflow is enabled for requests.
    /// </summary>
    public bool EnableSafeOverflowForRequests { get; }

    /// <summary>
    /// Gets whether safe overflow is enabled for responses.
    /// </summary>
    public bool EnableSafeOverflowForResponses { get; }

    /// <summary>
    /// Gets whether fire-and-forget requests are enabled.
    /// </summary>
    public bool EnableFireAndForgetRequests { get; }

    /// <summary>
    /// Gets the maximum number of active requests per client.
    /// </summary>
    public ulong MaxActiveRequestsPerClient { get; }

    /// <summary>
    /// Gets the maximum number of loaned requests.
    /// </summary>
    public ulong MaxLoanedRequests { get; }

    /// <summary>
    /// Gets the maximum response buffer size.
    /// </summary>
    public ulong MaxResponseBufferSize { get; }

    /// <summary>
    /// Gets the maximum number of servers.
    /// </summary>
    public ulong MaxServers { get; }

    /// <summary>
    /// Gets the maximum number of clients.
    /// </summary>
    public ulong MaxClients { get; }

    /// <summary>
    /// Gets the maximum number of nodes.
    /// </summary>
    public ulong MaxNodes { get; }

    /// <summary>
    /// Gets the maximum number of borrowed responses per pending response.
    /// </summary>
    public ulong MaxBorrowedResponsesPerPendingResponse { get; }

    internal RequestResponseStaticConfig(ref Iox2NativeMethods.iox2_static_config_request_response_t native)
    {
        EnableSafeOverflowForRequests = native.enable_safe_overflow_for_requests;
        EnableSafeOverflowForResponses = native.enable_safe_overflow_for_responses;
        EnableFireAndForgetRequests = native.enable_fire_and_forget_requests;
        MaxActiveRequestsPerClient = (ulong)native.max_active_requests_per_client;
        MaxLoanedRequests = (ulong)native.max_loaned_requests;
        MaxResponseBufferSize = (ulong)native.max_response_buffer_size;
        MaxServers = (ulong)native.max_servers;
        MaxClients = (ulong)native.max_clients;
        MaxNodes = (ulong)native.max_nodes;
        MaxBorrowedResponsesPerPendingResponse = (ulong)native.max_borrowed_responses_per_pending_response;
    }
}

/// <summary>
/// Static configuration for Blackboard messaging pattern services.
/// </summary>
public class BlackboardStaticConfig
{
    /// <summary>
    /// Gets the maximum number of readers.
    /// </summary>
    public ulong MaxReaders { get; }

    /// <summary>
    /// Gets the maximum number of writers.
    /// </summary>
    public ulong MaxWriters { get; }

    /// <summary>
    /// Gets the maximum number of nodes.
    /// </summary>
    public ulong MaxNodes { get; }

    internal BlackboardStaticConfig(ref Iox2NativeMethods.iox2_static_config_blackboard_t native)
    {
        MaxReaders = (ulong)native.max_readers;
        MaxWriters = (ulong)native.max_writers;
        MaxNodes = (ulong)native.max_nodes;
    }
}