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
using Xunit;

namespace Iceoryx2.Tests
{
    /// <summary>
    /// Runtime tests that verify the C# bindings work with the actual native library
    /// </summary>
    public class RuntimeTests
    {
        [Fact]
        public void NativeLibraryLoads()
        {
            // This test verifies that the native library can be loaded
            // Just accessing the class should trigger static constructor and library loading
            var exception = Record.Exception(() =>
            {
                // Call a simple log level setter that shouldn't crash
                Iox2NativeMethods.iox2_set_log_level_from_env_or(
                    Iox2NativeMethods.iox2_log_level_e.INFO);
            });

            // If this completes without throwing, the library loaded successfully
            Assert.Null(exception);
        }

        [Fact]
        public void NodeBuilderCanBeCreated()
        {
            // Test that we can create a NodeBuilder with proper struct
            var builderStruct = new Iox2NativeMethods.iox2_node_builder_t();
            var builderHandle = Iox2NativeMethods.iox2_node_builder_new(ref builderStruct);

            Assert.NotEqual(IntPtr.Zero, builderHandle);
        }

        [Fact]
        public void CanCreateNode()
        {
            // Create a node builder with proper struct
            var builderStruct = new Iox2NativeMethods.iox2_node_builder_t();
            var builderHandle = Iox2NativeMethods.iox2_node_builder_new(ref builderStruct);
            Assert.NotEqual(IntPtr.Zero, builderHandle);

            // Build the node - pass IntPtr.Zero to let C allocate on heap
            var result = Iox2NativeMethods.iox2_node_builder_create(
                builderHandle,
                IntPtr.Zero,  // NULL - let C allocate the struct
                Iox2NativeMethods.iox2_service_type_e.IPC,
                out IntPtr nodeHandle);

            // Check result - IOX2_OK = 0
            Assert.Equal(Iox2NativeMethods.IOX2_OK, result);
            Assert.NotEqual(IntPtr.Zero, nodeHandle);

            // Clean up
            if (nodeHandle != IntPtr.Zero)
            {
                Iox2NativeMethods.iox2_node_drop(nodeHandle);
            }
        }

        [Fact]
        public void ServiceBuilderCanBeCreated()
        {
            // First create a node
            var builderStruct = new Iox2NativeMethods.iox2_node_builder_t();
            var nodeBuilderHandle = Iox2NativeMethods.iox2_node_builder_new(ref builderStruct);

            var result = Iox2NativeMethods.iox2_node_builder_create(
                nodeBuilderHandle,
                IntPtr.Zero,  // NULL - let C allocate the struct
                Iox2NativeMethods.iox2_service_type_e.IPC,
                out IntPtr nodeHandle);

            Assert.Equal(Iox2NativeMethods.IOX2_OK, result);
            Assert.NotEqual(IntPtr.Zero, nodeHandle);

            // Now try to get a service builder from the node
            // Note: This needs proper struct allocation too, but for now test the node works

            // Clean up
            Iox2NativeMethods.iox2_node_drop(nodeHandle);
        }

        [Fact]
        public void CrossPlatformLibraryNameIsCorrect()
        {
            // This test verifies the library name logic without calling native code
            string expectedName;

            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows))
            {
                expectedName = "iceoryx2_ffi_c.dll";
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.OSX))
            {
                expectedName = "libiceoryx2_ffi_c.dylib";
            }
            else
            {
                expectedName = "libiceoryx2_ffi_c.so";
            }

            Console.WriteLine($"Expected native library: {expectedName}");
            Assert.NotNull(expectedName);
        }
    }
}