using SharpDX.Direct3D12;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Core.Graphics
{
  public class GPUResource
  {
    public Resource Resource => _Resource;
    public long GPUVirtualAddress => _GPUVirtualAddress;

    protected Resource _Resource;
    protected ResourceStates _UsageState = ResourceStates.Common;
    protected ResourceStates _TransitionState = ResourceStates.Common;
    protected long _GPUVirtualAddress = 0;

    IntPtr _UserAllocatedMemory = IntPtr.Zero;

    public GPUResource(Resource resource, ResourceStates currentState)
    {
      _Resource = resource;
      _UsageState = currentState;
    }

    public virtual void Destroy()
    {
      _Resource?.Dispose();
      _Resource = null;
      _GPUVirtualAddress = 0;

      if(_UserAllocatedMemory != null)
      {
        Marshal.FreeHGlobal(_UserAllocatedMemory);
        _UserAllocatedMemory = IntPtr.Zero;
      }
    }
  }
}
