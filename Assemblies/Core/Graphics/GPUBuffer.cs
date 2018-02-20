namespace Core.Graphics
{
  using SharpDX.Direct3D12;
  using System;
  using System.Diagnostics;

  /// <summary>
  /// Defines the <see cref="GPUBuffer" />
  /// </summary>
  public abstract class GPUBuffer : GPUResource
  {
    /// <summary>
    /// Gets the DescribeBuffer
    /// </summary>
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

    /// <summary>
    /// Gets the UAV
    /// </summary>
    public CpuDescriptorHandle UAV => _UAV;

    /// <summary>
    /// Gets the SRV
    /// </summary>
    public CpuDescriptorHandle SRV => _SRV;

    /// <summary>
    /// Gets the RootConstantBufferView
    /// </summary>
    public long RootConstantBufferView => _GPUVirtualAddress;

    /// <summary>
    /// Defines the _UAV
    /// </summary>
    protected CpuDescriptorHandle _UAV;

    /// <summary>
    /// Defines the _SRV
    /// </summary>
    protected CpuDescriptorHandle _SRV;

    /// <summary>
    /// Defines the _BufferSize
    /// </summary>
    protected long _BufferSize = 0;

    /// <summary>
    /// Defines the _ElementCount
    /// </summary>
    protected UInt32 _ElementCount = 0;

    /// <summary>
    /// Defines the _ElementSize
    /// </summary>
    protected UInt32 _ElementSize = 0;

    /// <summary>
    /// Defines the _ResourceFlags
    /// </summary>
    protected ResourceFlags _ResourceFlags = ResourceFlags.AllowUnorderedAccess;

    /// <summary>
    /// The CreateDerivedViews
    /// </summary>
    protected abstract void CreateDerivedViews();

    /// <summary>
    /// Initializes a new instance of the <see cref="GPUBuffer"/> class.
    /// </summary>
    /// <param name="resource">The <see cref="Resource"/></param>
    /// <param name="currentState">The <see cref="ResourceStates"/></param>
    public GPUBuffer(Resource resource, ResourceStates currentState) : base(resource, currentState)
    {
    }

    /// <summary>
    /// The Create
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name">The <see cref="string"/></param>
    /// <param name="numElements">The <see cref="uint"/></param>
    /// <param name="elementSize">The <see cref="uint"/></param>
    /// <param name="initialData">The <see cref="T[]"/></param>
    internal void Create<T>(string name, uint numElements, uint elementSize, T[] initialData) where T : struct
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

      _Resource = GraphicsCore.Device.CreateCommittedResource(heapProp, HeapFlags.None, resourceDesc, _UsageState, null);
      Debug.Assert(_Resource != null);
      _GPUVirtualAddress = _Resource.GPUVirtualAddress;

      if (initialData != null)
      {

      }
    }
  }
}
