using SharpDX.Direct3D12;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core
{
  public static class Constants
  {
    public const long DEFAULT_ALIGN = 256;
    public const int GPU_VIRTUAL_ADDRESS_UNKNOWN = -1;
    public const int VALID_COMPUTE_QUEUE_RESOURCE_STATES = (int)(ResourceStates.UnorderedAccess | ResourceStates.NonPixelShaderResource | ResourceStates.CopyDestination | ResourceStates.CopySource);
    public const int D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES = unchecked((int)0xffffffff);
  }
}
