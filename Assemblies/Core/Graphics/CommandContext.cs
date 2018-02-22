namespace Core.Graphics
{
  using SharpDX;
  using SharpDX.Direct3D12;
  using System;
  using System.Diagnostics;
  using System.Linq;
  using System.Runtime.InteropServices;

  /// <summary>
  /// Defines the <see cref="DWParam" />
  /// </summary>
  [StructLayout(LayoutKind.Explicit)]
  public struct DWParam
  {
    #region Fields

    /// <summary>
    /// Defines the Float
    /// </summary>
    [FieldOffset(0)]
    public float Float;

    /// <summary>
    /// Defines the Int
    /// </summary>
    [FieldOffset(0)]
    public int Int;

    /// <summary>
    /// Defines the int
    /// </summary>
    [FieldOffset(0)]
    public uint Uint;

    #endregion

    public static implicit operator DWParam(float f) => new DWParam { Float = f };
    public static implicit operator DWParam(uint u) => new DWParam { Uint = u };
    public static implicit operator DWParam(int i) => new DWParam { Int = i };
  }

  /// <summary>
  /// Defines the <see cref="CommandContext" />
  /// </summary>
  public class CommandContext : IDisposable
  {
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandContext"/> class.
    /// </summary>
    /// <param name="type">The <see cref="CommandListType"/></param>
    private CommandContext(CommandListType type)
    {
      _Type = type;
      _DynamicViewDescriptorHeap = new DynamicDescriptorHeap(this, DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
      _DynamicSamplerDescriptorHeap = new DynamicDescriptorHeap(this, DescriptorHeapType.Sampler);
      _CpuLinearAllocator = new LinearAllocator(ELinearAllocatorType.CpuWritable);
      _GpuLinearAllocator = new LinearAllocator(ELinearAllocatorType.GpuExclusive);
    }

    private void Reset()
    {
      Debug.Assert(_CommandList != null && _CurrentAllocator == null);
      _CurrentAllocator = Globals.CommandManager.Queue(_Type).RequestAllocator();
      _CommandList.Reset(_CurrentAllocator, null);

      _CurrentGraphicsRootSignature = null;
      _CurrentGraphicsPipelineState = null;
      _CurrentComputeRootSignature = null;
      _CurrentComputePipelineState = null;
      _NumBarrierToFlush = 0;

      BindDescriptorHeaps();
    }

    public static void DestroyAllContexts()
    {
      LinearAllocator.DestroyAll();
      DynamicDescriptorHeap.DestroyAll();
      Globals.ContextManager.DestroyAllContexts();
    }

    public static CommandContext Begin(string id = "")
    {
      var newContext = Globals.ContextManager.AllocateContext(CommandListType.Direct);
      newContext.ID = id;

      //TODO EngineProfiling
      return newContext;
    }

    // Flush existing commands to the GPU but keep the context alive
    public long Flush(bool waitForCompletion = false)
    {
      FlushResourceBarriers();
      Debug.Assert(_CurrentAllocator != null);
      var fenceValue = Globals.CommandManager.Queue(_Type).ExecuteCommandList(_CommandList);

      if (waitForCompletion) { CommandListManager.WaitForFence(fenceValue); }

      // Reset the command list and restore previous state

      _CommandList.Reset(_CurrentAllocator, null);

      if(_CurrentGraphicsRootSignature != null)
      {
        _CommandList.SetGraphicsRootSignature(_CurrentGraphicsRootSignature);
        _CommandList.PipelineState = _CurrentGraphicsPipelineState;
      }

      if (_CurrentComputeRootSignature != null)
      {
        _CommandList.SetComputeRootSignature(_CurrentComputeRootSignature);
        _CommandList.PipelineState = _CurrentComputePipelineState;
      }

      BindDescriptorHeaps();

      return fenceValue;
    }

    // Flush existing commands and release the current context
    public long Finish(bool waitForCompletion = false)
    {
      Debug.Assert(_Type == CommandListType.Direct || _Type == CommandListType.Compute);

      FlushResourceBarriers();

      //TODO : EngineProfiling

      Debug.Assert(_CurrentAllocator != null);

      var queue = Globals.CommandManager.Queue(_Type);
      var fenceValue = queue.ExecuteCommandList(_CommandList);
      queue.DiscardAllocator(fenceValue, _CurrentAllocator);
      _CurrentAllocator = null;

      _CpuLinearAllocator.CleanUpPages(fenceValue);
      _GpuLinearAllocator.CleanUpPages(fenceValue);
      _DynamicViewDescriptorHeap.CleanupUsedHeaps(fenceValue);
      _DynamicSamplerDescriptorHeap.CleanupUsedHeaps(fenceValue);

      if (waitForCompletion) { CommandListManager.WaitForFence(fenceValue); }

      Globals.ContextManager.FreeContext(this);

      return fenceValue;
    }

    public void Initialize()
    {
      Globals.CommandManager.CreateNewCommandList(_Type, out _CommandList, out _CurrentAllocator);
    }

    public GraphicsContext GraphicsContext {
      get {
        Debug.Assert(_Type != CommandListType.Compute, "Cannot convert async compute context to graphics");
        return this as GraphicsContext;
      }
    }

    public ComputeContext ComputeContext => this as ComputeContext;

    public CommandList CommandList => _CommandList;

    public void CopyBuffer(GPUResource dest, GPUResource src)
    {
      TransitionResource(dest, ResourceStates.CopyDestination);
      TransitionResource(src, ResourceStates.CopySource);
      FlushResourceBarriers();
      _CommandList.CopyResource(dest.Resource, src.Resource);
    }
    public void CopyBufferRegion(GPUResource dest, long destOffset, GPUResource src, long srcOffset, long numBytes)
    {
      TransitionResource(dest, ResourceStates.CopyDestination);
      //TransitionResource(src, ResourceStates.CopySource);
      FlushResourceBarriers();
      _CommandList.CopyBufferRegion(dest.Resource, destOffset, src.Resource, srcOffset, numBytes);
    }
    public void CopySubresource(GPUResource dest, int destSubIndex, GPUResource src, int srcSubIndex)
    {
      FlushResourceBarriers();
      var destLocation = new TextureCopyLocation(dest.Resource, destSubIndex);
      var srcLocation = new TextureCopyLocation(src.Resource, srcSubIndex);

      _CommandList.CopyTextureRegion(destLocation, 0, 0, 0, srcLocation, null);
    }
    public void CopyCounter(GPUResource dest, long destOffset, StructuredBuffer src)
    {
      TransitionResource(dest, ResourceStates.CopyDestination);
      TransitionResource(src, ResourceStates.CopySource);
      FlushResourceBarriers();
      _CommandList.CopyBufferRegion(dest.Resource, destOffset, src.CounterBuffer.Resource, 0, 4);
    }
    public void ResetCounter(StructuredBuffer buf, long value = 0)
    {
      FillBuffer(buf.CounterBuffer, 0, value, sizeof(UInt32));
      TransitionResource(buf.CounterBuffer, ResourceStates.UnorderedAccess);
    }

    public DynAlloc ReserveUploadMemory(long sizeInBytes) => _CpuLinearAllocator.Allocate(sizeInBytes);
    public static void InitializeTexture(GPUResource dest, int numSubresources, SubResourceInformation[] subData )
    {
      var uploadBufferSize = DirectX12.GetRequiredIntermediateSize(dest.Resource, 0, numSubresources);

      var initContext = Begin();

      var mem = initContext.ReserveUploadMemory(uploadBufferSize);
      DirectX12.UpdateSubResources(initContext._CommandList, dest.Resource, mem.Buffer.Resource, 0, 0, numSubresources, subData);
      initContext.Finish(true);
    }
    public static void InitializeBuffer(GPUResource dest, byte[] data, long numBytes, long offset = 0) {
      var initialContext = Begin();

      var mem = initialContext.ReserveUploadMemory(numBytes);
      var ptr = mem.Buffer.Resource.Map(0, null);
      Utilities.Write(ptr, data, 0, data.Length);
      mem.Buffer.Resource.Unmap(0);

      initialContext.TransitionResource(dest, ResourceStates.CopyDestination, true);
      initialContext._CommandList.CopyBufferRegion(dest.Resource, offset, mem.Buffer.Resource, 0, numBytes);
      initialContext.TransitionResource(dest, ResourceStates.GenericRead, true);
      initialContext.Finish(true);
    }
    public static void InitializeTextureArraySlice(GPUResource dest, int sliceIndex, GPUResource src) {
      var context = Begin();

      context.TransitionResource(dest, ResourceStates.CopyDestination);
      context.FlushResourceBarriers();

      var destDesc = dest.Resource.Description;
      var srcDesc = src.Resource.Description;

      Debug.Assert(sliceIndex < destDesc.DepthOrArraySize &&
        srcDesc.DepthOrArraySize == 1 &&
        destDesc.Width == srcDesc.Width &&
        destDesc.Height == srcDesc.Height &&
        destDesc.MipLevels <= srcDesc.MipLevels);

      int subresourceIndex = sliceIndex * destDesc.MipLevels;

      for(int idx = 0; idx < destDesc.MipLevels; idx++)
      {
        var destCopyLocation = new TextureCopyLocation(dest.Resource, subresourceIndex + idx);
        var srcCopyLocation = new TextureCopyLocation(src.Resource, idx);

        context._CommandList.CopyTextureRegion(destCopyLocation, 0, 0, 0, srcCopyLocation, null);
      }

      context.TransitionResource(dest, ResourceStates.GenericRead);
      context.Finish(true);
    }
    public static void ReadbackTexture2D(GPUResource readbackBuffer, PixelBuffer srcBuffer) {
      var desc = readbackBuffer.Resource.Description;
      var footprints = new PlacedSubResourceFootprint[1];
      Globals.Device.GetCopyableFootprints(ref desc, 0, 1, 0, footprints, new int[1], new long[1], out var totalBytes);

      var context = Begin("Copy texture to memory");
      context.TransitionResource(srcBuffer, ResourceStates.CopySource, true);
      context._CommandList.CopyTextureRegion(new TextureCopyLocation(readbackBuffer.Resource, footprints[0]), 0, 0, 0, new TextureCopyLocation(srcBuffer.Resource, 0), null);
      context.Finish(true);
    }

    public void WriteBuffer(GPUResource dest, long destOffset, byte[] data, long numBytes ) {
      Debug.Assert(data != null && MathUtils.IsAligned(data, 16));
      var tempSpace = _CpuLinearAllocator.Allocate(numBytes, 512);
      Utilities.Write(tempSpace.DataPtr, data, 0, data.Length);
      CopyBufferRegion(dest, destOffset, tempSpace.Buffer, tempSpace.Offset, numBytes);
    }
    public void FillBuffer(GPUResource dest, long destOffset, DWParam value, long numBytes) {
      var tempSpace = _CpuLinearAllocator.Allocate(numBytes, 512);

      var tmp = Enumerable.Repeat(value.Float, (int)numBytes / sizeof(float)).ToArray();

      Utilities.Write(tempSpace.DataPtr, tmp, 0, tmp.Length);
      CopyBufferRegion(dest, destOffset, tempSpace.Buffer, tempSpace.Offset, numBytes);
    }

    public void TransitionResource(GPUResource resource, ResourceStates newState, bool flushImmediate = false) {
      var oldState = resource.UsageState;

      if(_Type == CommandListType.Compute)
      {
        Debug.Assert((oldState & (ResourceStates)Constants.VALID_COMPUTE_QUEUE_RESOURCE_STATES) == oldState);
        Debug.Assert((newState & (ResourceStates)Constants.VALID_COMPUTE_QUEUE_RESOURCE_STATES) == newState);

        if(oldState != newState)
        {
          Debug.Assert(_NumBarrierToFlush < 16, "Exceeded arbitrary limit on buffered barriers");
          ref var barrierDesc = ref _ResourceBarrierBuffer[_NumBarrierToFlush++];
          barrierDesc.Type = ResourceBarrierType.Transition;
          barrierDesc.Transition = new ResourceTransitionBarrier(resource.Resource, oldState, newState) { Subresource = Constants.D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES };

          if(newState == resource.TransitionState)
          {
            barrierDesc.Flags = ResourceBarrierFlags.EndOnly;
            resource.TransitionState = (ResourceStates)(- 1);
          }
          else { barrierDesc.Flags = ResourceBarrierFlags.None; }

          resource.UsageState = newState;
        }
        else if(newState == ResourceStates.UnorderedAccess) { InsertUAVBarrier(resource, flushImmediate); }

        if(flushImmediate || _NumBarrierToFlush == 16) { FlushResourceBarriers(); }
      }
    }
    public void BeginResourceTransition(GPUResource resource, ResourceStates newState, bool flushImmediate = false) { }
    public void InsertUAVBarrier(GPUResource resource, bool FlushImmediate = false) { }
    public void InsertAliasBarrier(GPUResource before, GPUResource after, bool flushImmediate = false) { }
    public void FlushResourceBarriers() { }

    public void InsertTimeStamp(QueryHeap queryHeap, int queryIdx) { }
    public void ResolveTimeStamps(Resource readbackHeap, QueryHeap queryHeap, int numQueries) { }
    public void PIXBeginEvent(string label) { }
    public void PIXEndEvent() { }
    public void PIXSetMarker(string label) { }

    public void SetDescriptorHeap(DescriptorHeapType type, DescriptorHeap heapPtr) { }
    public void SetDescriptorHeaps(int heapCount, DescriptorHeapType[], DescriptorHeap heapPtrs[] ) { }

    public void SetPredication(Resource buffer, long bufferOffset, PredicationOperation op) { }

    protected void BindDescriptorHeaps()
    {
      var nonNullHeaps = 0;
      var heapsToBind = new DescriptorHeap[(int)DescriptorHeapType.NumTypes];

      for(int idx = 0; idx < (int)DescriptorHeapType.NumTypes; idx++)
      {
        var heap = _CurrentDescriptorHeap[idx];
        if(heap != null) { heapsToBind[nonNullHeaps++] = heap; }
      }

      if(nonNullHeaps > 0) { _CommandList.SetDescriptorHeaps(heapsToBind); }
    }

    #endregion

    #region Properties

    protected CommandListManager _OwningManager;
    protected GraphicsCommandList _CommandList;
    protected CommandAllocator _CurrentAllocator;

    protected RootSignature _CurrentGraphicsRootSignature;
    protected PipelineState _CurrentGraphicsPipelineState;
    protected RootSignature _CurrentComputeRootSignature;
    protected PipelineState _CurrentComputePipelineState;

    protected DynamicDescriptorHeap _DynamicViewDescriptorHeap;
    protected DynamicDescriptorHeap _DynamicSamplerDescriptorHeap;

    protected ResourceBarrier[] _ResourceBarrierBuffer = new ResourceBarrier[16];
    protected int _NumBarrierToFlush;

    protected DescriptorHeap[] _CurrentDescriptorHeap = new DescriptorHeap[(int)DescriptorHeapType.NumTypes];

    protected LinearAllocator _CpuLinearAllocator;
    protected LinearAllocator _GpuLinearAllocator;

    protected string ID { get; set; }
    protected CommandListType _Type;

    #endregion

    #region Methods
    
    public void Dispose()
    {
      //TODO implement
    }

    #endregion
  }
}
