using Iceoryx2.SafeHandles;
using System;

namespace Iceoryx2;

/// <summary>
/// Builder for creating a Node.
/// </summary>
public sealed class NodeBuilder
{
    private string? _name;

    private NodeBuilder()
    {
    }

    /// <summary>
    /// Creates a new NodeBuilder.
    /// </summary>
    public static NodeBuilder New() => new();

    /// <summary>
    /// Sets the name of the node.
    /// </summary>
    public NodeBuilder Name(string name)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        return this;
    }

    /// <summary>
    /// Creates the node.
    /// </summary>
    public Result<Node, Iox2Error> Create()
    {
        try
        {
            // Create node builder with proper struct
            var builderStruct = new Native.Iox2NativeMethods.iox2_node_builder_t();
            var builderHandle = Native.Iox2NativeMethods.iox2_node_builder_new(ref builderStruct);

            if (builderHandle == IntPtr.Zero)
                return Result<Node, Iox2Error>.Err(Iox2Error.NodeCreationFailed);

            // Set node name if provided
            if (!string.IsNullOrEmpty(_name))
            {
                var result = Native.Iox2NativeMethods.iox2_node_name_new(
                    IntPtr.Zero,  // NULL - let C allocate the struct
                    _name,
                    System.Text.Encoding.UTF8.GetByteCount(_name),
                    out var nodeNameHandle);

                if (result == Native.Iox2NativeMethods.IOX2_OK)
                {
                    var nodeNamePtr = Native.Iox2NativeMethods.iox2_cast_node_name_ptr(nodeNameHandle);
                    Native.Iox2NativeMethods.iox2_node_builder_set_name(ref builderHandle, nodeNamePtr);
                    Native.Iox2NativeMethods.iox2_node_name_drop(nodeNameHandle);
                }
            }

            // Create the node - pass IntPtr.Zero to let C FFI allocate the struct
            var serviceType = Native.Iox2NativeMethods.iox2_service_type_e.IPC;
            var createResult = Native.Iox2NativeMethods.iox2_node_builder_create(
                builderHandle,
                IntPtr.Zero,  // NULL - let C allocate the struct on heap
                serviceType,
                out var nodeHandle);

            if (createResult != Native.Iox2NativeMethods.IOX2_OK || nodeHandle == IntPtr.Zero)
                return Result<Node, Iox2Error>.Err(Iox2Error.NodeCreationFailed);

            var handle = new SafeNodeHandle(nodeHandle);
            var node = new Node(handle, serviceType);

            return Result<Node, Iox2Error>.Ok(node);
        }
        catch (Exception)
        {
            return Result<Node, Iox2Error>.Err(Iox2Error.NodeCreationFailed);
        }
    }
}