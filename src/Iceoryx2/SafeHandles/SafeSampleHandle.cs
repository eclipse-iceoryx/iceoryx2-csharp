using System;

namespace Iceoryx2.SafeHandles;

/// <summary>
/// Safe handle for Sample resources.
/// </summary>
internal sealed class SafeSampleHandle : SafeIox2Handle
{
    private readonly bool _isMutable;

    public bool IsMutable => _isMutable;

    public SafeSampleHandle(bool isMutable = false) : base()
    {
        _isMutable = isMutable;
    }

    public SafeSampleHandle(IntPtr handle, bool isMutable = false) : base(handle)
    {
        _isMutable = isMutable;
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid && handle != IntPtr.Zero)
        {
            if (_isMutable)
                Native.Iox2NativeMethods.iox2_sample_mut_drop(handle);
            else
                Native.Iox2NativeMethods.iox2_sample_drop(handle);
            return true;
        }
        return false;
    }
}