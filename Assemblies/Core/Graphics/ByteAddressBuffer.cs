using SharpDX.Direct3D12;
using System;

namespace Core.Graphics
{
  public class ByteAddressBuffer : GPUBuffer
  {
    public ByteAddressBuffer(Resource resource, ResourceStates currentState) : base(resource, currentState) { }
    public override void CreateDerivedViews()
    {
      var srvDesc = new ShaderResourceViewDescription
      {
        Dimension = ShaderResourceViewDimension.Buffer,
        Format = SharpDX.DXGI.Format.R32_Typeless,
        Shader4ComponentMapping = ShaderComponentMapping.DefaultComponentMapping(),
        Buffer = new ShaderResourceViewDescription.BufferResource { ElementCount = _BufferSize / 4, Flags = BufferShaderResourceViewFlags.Raw }
      };

      if (_SRV.Ptr == Constants.GPU_VIRTUAL_ADDRESS_UNKNOWN) { _SRV = Globals.AllocateDescriptor(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView); }

      Globals.Device.CreateShaderResourceView(_Resource, srvDesc, _SRV);

      var uavDesc = new UnorderedAccessViewDescription
      {
        Dimension = UnorderedAccessViewDimension.Buffer,
        Format = SharpDX.DXGI.Format.R32_Typeless,
        Buffer = new UnorderedAccessViewDescription.BufferResource { ElementCount = _BufferSize / 4, Flags = BufferUnorderedAccessViewFlags.Raw }
      };

      if(_UAV.Ptr == Constants.GPU_VIRTUAL_ADDRESS_UNKNOWN) { _UAV = Globals.AllocateDescriptor(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView); }

      Globals.Device.CreateUnorderedAccessView(_Resource, null, uavDesc, _UAV);
    }
  }
}