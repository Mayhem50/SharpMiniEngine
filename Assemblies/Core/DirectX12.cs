using SharpDX.Direct3D12;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core
{
  public static class DirectX12
  {
    public static long GetRequiredIntermediateSize(Resource destResource, int firstSubResource, int numSubResource)
    {
      var desc = destResource.Description;
      long requiredSize = 0;
      destResource.GetDevice(SharpDX.Utilities.GetGuidFromType(typeof(Device)), out var ptr);

      //TODO : Verify that dispose don't release C++ PTR
      using (var device = new Device(ptr))
      {
        device.GetCopyableFootprints(ref desc, firstSubResource, numSubResource, 0, null, null, null, out requiredSize);
      }

      return requiredSize;
    }
  }
}
