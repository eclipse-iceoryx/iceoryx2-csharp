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

using Iceoryx2;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace Iceoryx2.Tests
{
    public class ZeroCopyTests
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct TestData
        {
            public int Id;
            public double Value;
        }

        [Fact]
        public void ZeroCopy_Write_ModifiesSharedMemoryDirectly()
        {
            var node = NodeBuilder.New()
                .Name("zero_copy_write_test")
                .Create()
                .Unwrap();

            var service = node.ServiceBuilder()
                .PublishSubscribe<TestData>()
                .Open("zero_copy_write_service")
                .Unwrap();

            var publisher = service.PublisherBuilder().Create().Unwrap();

            // Loan a sample
            var loanResult = publisher.Loan<TestData>();
            Assert.True(loanResult.IsOk);

            using var sample = loanResult.Unwrap();

            // Get reference to payload
            ref var payload = ref sample.GetPayloadRef();

            // Modify via reference
            payload.Id = 123;
            payload.Value = 456.789;

            // Verify modification in the sample (reading back via property which copies)
            var copy = sample.Payload;
            Assert.Equal(123, copy.Id);
            Assert.Equal(456.789, copy.Value);
        }

        [Fact]
        public async Task ZeroCopy_Read_AccessesSharedMemoryDirectly()
        {
            var node = NodeBuilder.New()
                .Name("zero_copy_read_test")
                .Create()
                .Unwrap();

            var service = node.ServiceBuilder()
                .PublishSubscribe<TestData>()
                .Open("zero_copy_read_service")
                .Unwrap();

            var publisher = service.PublisherBuilder().Create().Unwrap();
            var subscriber = service.SubscriberBuilder().Create().Unwrap();

            // Publish data
            publisher.Send(new TestData { Id = 999, Value = 1.23 }).Unwrap();

            // Receive data
            var result = await subscriber.ReceiveAsync<TestData>(TimeSpan.FromSeconds(1));
            Assert.True(result.IsOk);

            using var sample = result.Unwrap();
            Assert.NotNull(sample);

            // Access via reference
            ref readonly var payload = ref sample.GetPayloadRefReadOnly();

            Assert.Equal(999, payload.Id);
            Assert.Equal(1.23, payload.Value);
        }
    }
}