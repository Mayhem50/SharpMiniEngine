using SharpDX.Direct3D12;

namespace Core.Graphics
{
  public class StructuredBuffer : GPUBuffer
  {
    public StructuredBuffer(Resource resource, ResourceStates currentState) : base(resource, currentState) { }

    public override void Destroy()
    {
      _CounterBuffer.Destroy();
      base.Destroy();
    }

    private ByteAddressBuffer _CounterBuffer;
    public ByteAddressBuffer CounterBuffer => _CounterBuffer;
    public override void CreateDerivedViews()
    {
      var srvDesc = new ShaderResourceViewDescription
      {
        Dimension = ShaderResourceViewDimension.Buffer,
        Format = SharpDX.DXGI.Format.Unknown,
        Buffer = new ShaderResourceViewDescription.BufferResource
        {
          FirstElement = 0,
          ElementCount = _ElementCount,
          StructureByteStride = _ElementSize,
          Flags = BufferShaderResourceViewFlags.None
        }
      };

      if(_SRV.Ptr == Constants.GPU_VIRTUAL_ADDRESS_UNKNOWN) { _SRV = Globals.AllocateDescriptor(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView); }
      Globals.Device.CreateShaderResourceView(_Resource, srvDesc, _SRV);

      var uavDesc = new UnorderedAccessViewDescription
      {
        Dimension = UnorderedAccessViewDimension.Buffer,
        Format = SharpDX.DXGI.Format.Unknown,
        Buffer = new UnorderedAccessViewDescription.BufferResource
        {
          CounterOffsetInBytes = 0,
          ElementCount = _ElementCount,
          StructureByteStride = _ElementSize,
          Flags = BufferUnorderedAccessViewFlags.None
        }
      };

      _CounterBuffer.Create("StructuredBuffer.Counter", 1, 4);

      if(_UAV.Ptr == Constants.GPU_VIRTUAL_ADDRESS_UNKNOWN) { _UAV = Globals.AllocateDescriptor(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView); }
      Globals.Device.CreateUnorderedAccessView(_Resource, _CounterBuffer.Resource, uavDesc, _UAV);
    }

    public CpuDescriptorHandle CounterSRV(CommandContext context)
    {
      context.TransitionResource(_CounterBuffer, ResourceStates.GenericRead);
      return _CounterBuffer.SRV;
    }

    public CpuDescriptorHandle CounterUAV(CommandContext context)
    {
      context.TransitionResource(_CounterBuffer, ResourceStates.UnorderedAccess);
      return _CounterBuffer.UAV;

    }
  }
}