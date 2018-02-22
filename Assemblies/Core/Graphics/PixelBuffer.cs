using SharpDX.Direct3D12;

namespace Core.Graphics
{
  public class PixelBuffer : GPUResource
  {
    public PixelBuffer(Resource resource, ResourceStates usage) : base(resource, usage)
    {
    }
  }
}