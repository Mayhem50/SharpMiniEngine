namespace Core.Graphics
{
  using SharpDX.Direct3D12;
  using System;
  using System.Diagnostics;

  /// <summary>
  /// Defines the <see cref="GPUBuffer" />
  /// </summary>
  public abstract class GPUBuffer : GPUResource, IDisposable
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
    protected int _BufferSize = 0;

    /// <summary>
    /// Defines the _ElementCount
    /// </summary>
    protected int _ElementCount = 0;

    /// <summary>
    /// Defines the _ElementSize
    /// </summary>
    protected int _ElementSize = 0;

    /// <summary>
    /// Defines the _ResourceFlags
    /// </summary>
    protected ResourceFlags _ResourceFlags = ResourceFlags.AllowUnorderedAccess;

    /// <summary>
    /// The CreateDerivedViews
    /// </summary>
    public abstract void CreateDerivedViews();

    /// <summary>
    /// Initializes a new instance of the <see cref="GPUBuffer"/> class.
    /// </summary>
    /// <param name="resource">The <see cref="Resource"/></param>
    /// <param name="currentState">The <see cref="ResourceStates"/></param>
    protected GPUBuffer(Resource resource, ResourceStates currentState) : base(resource, currentState)
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
    internal void Create(string name, int numElements, int elementSize, byte[] initialData = null)
    {
      base.Destroy();

      _ElementCount = numElements;
      _ElementSize = elementSize;
      _BufferSize = numElements * elementSize;

      var resourceDesc = DescribeBuffer;
      _UsageState = ResourceStates.Common;

      var heapProp = new HeapProperties
      {
        Type = HeapType.Default,
        CPUPageProperty = CpuPageProperty.Unknown,
        CreationNodeMask = 1,
        VisibleNodeMask = 1
      };

      _Resource = Globals.Device.CreateCommittedResource(heapProp, HeapFlags.None, resourceDesc, _UsageState, null);
      _Resource.Name = name;
      Debug.Assert(_Resource != null);
      _GPUVirtualAddress = _Resource.GPUVirtualAddress;

      if (initialData != null) { CommandContext.InitializeBuffer(this, initialData, _BufferSize); }
    }

    public void Dispose()
    {
      Destroy();
      GC.SuppressFinalize(this);
    }
  }
}
