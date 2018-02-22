using SharpDX;
using SharpDX.Direct3D12;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Core
{
  public static class DirectX12
  {
    public static long GetRequiredIntermediateSize(Resource destResource, int firstSubResource, int numSubResource)
    {
      var desc = destResource.Description;
      long requiredSize = 0;
      destResource.GetDevice(Utilities.GetGuidFromType(typeof(Device)), out var ptr);

      //TODO : Verify that dispose don't release C++ PTR
      using (var device = new Device(ptr))
      {
        device.GetCopyableFootprints(ref desc, firstSubResource, numSubResource, 0, null, null, null, out requiredSize);
      }

      return requiredSize;
    }

    public static long UpdateSubResources(GraphicsCommandList cmdList, Resource destResource, Resource intermediateResource, long intermediateOffset, int firstSubresource, int numSubresource, SubResourceInformation[] datas)
    {
      long memToAlloc = (Utilities.SizeOf<PlacedSubResourceFootprint>() + sizeof(uint) + sizeof(UInt64)) * numSubresource;
      var layouts = new PlacedSubResourceFootprint[numSubresource];
      var numRows = new int[numSubresource];
      var rowSizesInBytes = new long[numSubresource];

      destResource.GetDevice(Utilities.GetGuidFromType(typeof(Device)), out var ptr);
      long result = 0;
      using (var device = new Device(ptr))
      {
        var desc = destResource.Description;
        device.GetCopyableFootprints(ref desc, firstSubresource, numSubresource, intermediateOffset, layouts, numRows, rowSizesInBytes, out var requiredSize);
        result = UpdateSubResources(cmdList, destResource, intermediateResource, firstSubresource, numSubresource, requiredSize, layouts, rowSizesInBytes, datas);
      }

      return result;
    }

    public static long UpdateSubResources(GraphicsCommandList cmdList, Resource destResource, Resource intermediateResource, int firstSubresource, int numSubresource, long requiredSize, PlacedSubResourceFootprint[] layouts, long[] rowSizesInBytes, SubResourceInformation[] datas)
    {
      var intermediateDesc = intermediateResource.Description;
      var destDesc = destResource.Description;
      var dataPtr = Marshal.UnsafeAddrOfPinnedArrayElement(datas, 0);

      if(intermediateDesc.Dimension != ResourceDimension.Buffer ||
        intermediateDesc.Width < requiredSize + layouts[0].Offset ||
        requiredSize == 0 ||
        (destDesc.Dimension == ResourceDimension.Buffer && 
        (firstSubresource != 0) || numSubresource != 1))
      {
        return 0;
      }

      var ptr = intermediateResource.Map(0, null);
      if(ptr == IntPtr.Zero) { return 0; }

      for(int idx = 0; idx < numSubresource; idx++)
      {
        if(rowSizesInBytes[idx] == 0) { return 0; }
        intermediateResource.WriteToSubresource(idx, null, new IntPtr(datas[idx].Offset), datas[idx].RowPitch, datas[idx].DepthPitch);
      }

      if(destDesc.Dimension == ResourceDimension.Buffer)
      {
        cmdList.CopyBufferRegion(destResource, 0, intermediateResource, layouts[0].Offset, layouts[0].Footprint.Width);
      }
      else
      {
        for(int idx = 0; idx < numSubresource; idx++)
        {
          var dest = new TextureCopyLocation(destResource, idx + firstSubresource);
          var src = new TextureCopyLocation(intermediateResource, layouts[idx]);
          cmdList.CopyTextureRegion(dest, 0, 0, 0, src, null);
        }
      }

      return requiredSize;
    }
  }
}
