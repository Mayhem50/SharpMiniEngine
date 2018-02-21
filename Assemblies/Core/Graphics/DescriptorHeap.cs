namespace Core.Graphics
{
  using SharpDX.Direct3D12;
  using System.Collections.Generic;
  using System.Diagnostics;

  /// <summary>
  /// Defines the <see cref="DescriptorAllocator" />
  /// </summary>
  public class DescriptorAllocator
  {
    /// <summary>
    /// Defines the _Type
    /// </summary>
    protected DescriptorHeapType _Type;

    /// <summary>
    /// Defines the _CurrentHeap
    /// </summary>
    protected DescriptorHeap _CurrentHeap = null;

    /// <summary>
    /// Defines the _CurrentHandle
    /// </summary>
    protected CpuDescriptorHandle _CurrentHandle;

    /// <summary>
    /// Defines the _DescriptorSize
    /// </summary>
    protected int _DescriptorSize;

    /// <summary>
    /// Defines the _RemainingFreeHandles
    /// </summary>
    protected int _RemainingFreeHandles;

    /// <summary>
    /// Defines the _NumDescriptorsPerHeap
    /// </summary>
    protected static readonly int _NumDescriptorsPerHeap = 256;

    /// <summary>
    /// Defines the _AllocationMutex
    /// </summary>
    internal static object _AllocationMutex = new object();

    /// <summary>
    /// Defines the _DescriptorHeapPool
    /// </summary>
    internal static List<DescriptorHeap> _DescriptorHeapPool;

    /// <summary>
    /// The RequestNewHeap
    /// </summary>
    /// <param name="type">The <see cref="DescriptorHeapType"/></param>
    /// <returns>The <see cref="DescriptorHeap"/></returns>
    protected static DescriptorHeap RequestNewHeap(DescriptorHeapType type)
    {
      lock (_AllocationMutex)
      {
        DescriptorHeapDescription desc = new DescriptorHeapDescription
        {
          Type = type,
          DescriptorCount = _NumDescriptorsPerHeap,
          Flags = DescriptorHeapFlags.None,
          NodeMask = 1
        };

        var heap = Globals.Device.CreateDescriptorHeap(desc);
        Debug.Assert(heap != null);
        _DescriptorHeapPool.Add(heap);
        return heap;
      }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DescriptorAllocator"/> class.
    /// </summary>
    /// <param name="type">The <see cref="DescriptorHeapType"/></param>
    public DescriptorAllocator(DescriptorHeapType type)
    {
      _Type = type;
    }

    /// <summary>
    /// The Allocate
    /// </summary>
    /// <param name="count">The <see cref="int"/></param>
    /// <returns>The <see cref="CpuDescriptorHandle"/></returns>
    public CpuDescriptorHandle Allocate(int count)
    {
      if (_CurrentHeap == null || _RemainingFreeHandles < count)
      {
        _CurrentHeap = RequestNewHeap(_Type);
        _CurrentHandle = _CurrentHeap.CPUDescriptorHandleForHeapStart;
        _RemainingFreeHandles = _NumDescriptorsPerHeap;

        if (_DescriptorSize == 0)
        {
          _DescriptorSize = Globals.Device.GetDescriptorHandleIncrementSize(_Type);
        }
      }

      CpuDescriptorHandle ret = _CurrentHandle;
      _CurrentHandle.Ptr += count * _DescriptorSize;
      _RemainingFreeHandles -= count;
      return ret;
    }
  }

  /// <summary>
  /// Defines the <see cref="DescriptorHandle" />
  /// </summary>
  public class DescriptorHandle
  {
    /// <summary>
    /// Defines the _CPUHandle
    /// </summary>
    private CpuDescriptorHandle _CPUHandle;

    /// <summary>
    /// Defines the _GPUHandle
    /// </summary>
    private GpuDescriptorHandle _GPUHandle;

    /// <summary>
    /// Initializes a new instance of the <see cref="DescriptorHandle"/> class.
    /// </summary>
    public DescriptorHandle()
    {
      _CPUHandle.Ptr = -1;
      _GPUHandle.Ptr = -1;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DescriptorHandle"/> class.
    /// </summary>
    /// <param name="cpuHandle">The <see cref="CpuDescriptorHandle"/></param>
    public DescriptorHandle(CpuDescriptorHandle cpuHandle)
    {
      _CPUHandle = cpuHandle;
      _GPUHandle.Ptr = -1;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DescriptorHandle"/> class.
    /// </summary>
    /// <param name="cpuHandle">The <see cref="CpuDescriptorHandle"/></param>
    /// <param name="gpuHandle">The <see cref="GpuDescriptorHandle"/></param>
    public DescriptorHandle(CpuDescriptorHandle cpuHandle, GpuDescriptorHandle gpuHandle)
    {
      _CPUHandle = cpuHandle;
      _GPUHandle = gpuHandle;
    }

    /// <summary>
    /// Gets the CPUHandle
    /// </summary>
    public CpuDescriptorHandle CPUHandle => _CPUHandle;

    /// <summary>
    /// Gets the GPUHandle
    /// </summary>
    public GpuDescriptorHandle GPUHandle => _GPUHandle;

    /// <summary>
    /// Gets a value indicating whether IsNull
    /// </summary>
    public bool IsNull => _CPUHandle.Ptr == -1;

    /// <summary>
    /// Gets a value indicating whether IsShaderVisible
    /// </summary>
    public bool IsShaderVisible => _GPUHandle.Ptr != -1;


    public static DescriptorHandle operator +(DescriptorHandle handle, int offsetScaledByDescriptorSize)
    {
      DescriptorHandle ret = new DescriptorHandle(handle._CPUHandle, handle._GPUHandle);
      if (handle._CPUHandle.Ptr != -1) { handle._CPUHandle.Ptr += offsetScaledByDescriptorSize; }
      if (handle._GPUHandle.Ptr != -1) { handle._GPUHandle.Ptr += offsetScaledByDescriptorSize; }
      return handle;
    }
  }

  /// <summary>
  /// Defines the <see cref="UserDescriptorHeap" />
  /// </summary>
  public class UserDescriptorHeap
  {
    /// <summary>
    /// Defines the _Heap
    /// </summary>
    private DescriptorHeap _Heap;

    /// <summary>
    /// Defines the _HeapDesc
    /// </summary>
    private DescriptorHeapDescription _HeapDesc;

    /// <summary>
    /// Defines the _DescriptorSize
    /// </summary>
    private int _DescriptorSize;

    /// <summary>
    /// Defines the _NumFreeDescriptors
    /// </summary>
    private int _NumFreeDescriptors;

    /// <summary>
    /// Defines the _FirstHandle
    /// </summary>
    private DescriptorHandle _FirstHandle;

    /// <summary>
    /// Defines the _NextHandle
    /// </summary>
    private DescriptorHandle _NextHandle;

    /// <summary>
    /// Gets the Heap
    /// </summary>
    public DescriptorHeap Heap => _Heap;

    /// <summary>
    /// The HasAvailableSpace
    /// </summary>
    /// <param name="count">The <see cref="int"/></param>
    /// <returns>The <see cref="bool"/></returns>
    public bool HasAvailableSpace(int count) => count <= _NumFreeDescriptors;

    /// <summary>
    /// The HandleAtOffset
    /// </summary>
    /// <param name="offset">The <see cref="int"/></param>
    /// <returns>The <see cref="DescriptorHandle"/></returns>
    public DescriptorHandle HandleAtOffset(int offset) => _FirstHandle + offset * _DescriptorSize;

    /// <summary>
    /// The ValidateHandle
    /// </summary>
    /// <param name="handle">The <see cref="DescriptorHandle"/></param>
    /// <returns>The <see cref="bool"/></returns>
    public bool ValidateHandle(in DescriptorHandle handle)
    {
      if (handle.CPUHandle.Ptr < _FirstHandle.CPUHandle.Ptr ||
        handle.CPUHandle.Ptr >= _FirstHandle.CPUHandle.Ptr + _HeapDesc.DescriptorCount * _DescriptorSize)
      {
        return false;
      }
      if (handle.GPUHandle.Ptr - _FirstHandle.GPUHandle.Ptr !=
       handle.CPUHandle.Ptr - _FirstHandle.CPUHandle.Ptr)
      {
        return false;
      }
      return true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserDescriptorHeap"/> class.
    /// </summary>
    /// <param name="type">The <see cref="DescriptorHeapType"/></param>
    /// <param name="maxCount">The <see cref="int"/></param>
    public UserDescriptorHeap(DescriptorHeapType type, int maxCount)
    {
      _HeapDesc.Type = type;
      _HeapDesc.DescriptorCount = maxCount;
      _HeapDesc.Flags = DescriptorHeapFlags.ShaderVisible;
      _HeapDesc.NodeMask = 1;
    }

    /// <summary>
    /// The Create
    /// </summary>
    /// <param name="debugHeapName">The <see cref="string"/></param>
    public void Create(string debugHeapName)
    {
      _Heap = Globals.Device.CreateDescriptorHeap(_HeapDesc);
      _Heap.Name = debugHeapName;
      _DescriptorSize = Globals.Device.GetDescriptorHandleIncrementSize(_HeapDesc.Type);
      _NumFreeDescriptors = _HeapDesc.DescriptorCount;
      _FirstHandle = new DescriptorHandle(_Heap.CPUDescriptorHandleForHeapStart, _Heap.GPUDescriptorHandleForHeapStart);
      _NextHandle = _FirstHandle;
    }

    /// <summary>
    /// The Alloc
    /// </summary>
    /// <param name="count">The <see cref="int"/></param>
    /// <returns>The <see cref="DescriptorHandle"/></returns>
    internal DescriptorHandle Alloc(int count)
    {
      Debug.Assert(HasAvailableSpace(count), "Descriptor Heap out of space.  Increase heap size.");
      DescriptorHandle ret = _NextHandle;
      _NextHandle += count * _DescriptorSize;
      return ret;
    }
  }
}
