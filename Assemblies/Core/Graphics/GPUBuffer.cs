using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SharpDX.Direct3D12;

namespace Core.Graphics
{
  public abstract class GPUBuffer : GPUResource
  {
    public ResourceDescription DescribeBuffer {
      get {
        Debug.Assert(_BufferSize != 0);
        return new ResourceDescription
        {
          Alignment = 0,
          DepthOrArraySize = 1,
          Dimension = ResourceDimension.Buffer,
          Flags = _ResourceFlags,
          Format = SharpDX.DXGI.Format.Unknown,
          Height = 1,
          Layout = TextureLayout.RowMajor,
          MipLevels = 1,
          SampleDescription = new SharpDX.DXGI.SampleDescription { Count = 1, Quality = 0 },
          Width = _BufferSize
        };
      }
    }

    public CpuDescriptorHandle UAV => _UAV;
    public CpuDescriptorHandle SRV => _SRV;
    public long RootConstantBufferView => _GPUVirtualAddress;

    protected CpuDescriptorHandle _UAV;
    protected CpuDescriptorHandle _SRV;
    
    protected long _BufferSize = 0;
    protected UInt32 _ElementCount = 0;
    protected UInt32 _ElementSize = 0;
    protected ResourceFlags _ResourceFlags = ResourceFlags.AllowUnorderedAccess;

    protected abstract void CreateDerivedViews();

    public GPUBuffer(Resource resource, ResourceStates currentState) : base(resource, currentState)
    {
    }

    void Create<T>(string name, uint numElements, uint elementSize, T[] initialData) where T : struct
    {
      base.Destroy();

      _ElementCount = numElements;
      _ElementSize = elementSize;
      _BufferSize = numElements * elementSize;

      var resourceDesc = DescribeBuffer;
      _UsageState = ResourceStates.Common;

      HeapProperties heapProp = new HeapProperties();
      heapProp.Type = HeapType.Default;
      heapProp.CPUPageProperty = CpuPageProperty.Unknown;
      heapProp.CreationNodeMask = 1;
      heapProp.VisibleNodeMask = 1;


    }
  }
}
