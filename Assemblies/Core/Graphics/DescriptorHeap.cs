using SharpDX.Direct3D12;
using System.Collections.Generic;
using System.Diagnostics;

namespace Core.Graphics
{
  public class DescriptorAllocator
  {
    protected DescriptorHeapType _Type;
    protected DescriptorHeap _CurrentHeap = null;
    protected CpuDescriptorHandle _CurrentHandle;
    protected int _DescriptorSize;
    protected int _RemainingFreeHandles;

    protected static readonly int _NumDescriptorsPerHeap = 256;
    static object _AllocationMutex = new object();
    static List<DescriptorHeap> _DescriptorHeapPool;
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

        var heap = GraphicsCore.Device.CreateDescriptorHeap(desc);
        Debug.Assert(heap != null);
        _DescriptorHeapPool.Add(heap);
        return heap;
      }
    }

    public DescriptorAllocator(DescriptorHeapType type)
    {
      _Type = type;
    }

    public CpuDescriptorHandle Allocate(int count)
    {
      if(_CurrentHeap == null || _RemainingFreeHandles < count)
      {
        _CurrentHeap = RequestNewHeap(_Type);
        _CurrentHandle = _CurrentHeap.CPUDescriptorHandleForHeapStart;
        _RemainingFreeHandles = _NumDescriptorsPerHeap;

        if(_DescriptorSize == 0)
        {
          _DescriptorSize = GraphicsCore.Device.GetDescriptorHandleIncrementSize(_Type);
        }
      }

      CpuDescriptorHandle ret = _CurrentHandle;
      _CurrentHandle.Ptr += count * _DescriptorSize;
      _RemainingFreeHandles -= count;
      return ret;
    }
  }

  public class DescriptorHandle
  {
    private CpuDescriptorHandle _CPUHandle;
    private GpuDescriptorHandle _GPUHandle;
    public DescriptorHandle()
    {
      _CPUHandle.Ptr = -1;
      _GPUHandle.Ptr = -1;
    }
    public DescriptorHandle(CpuDescriptorHandle cpuHandle)
    {
      _CPUHandle = cpuHandle;
      _GPUHandle.Ptr = -1;
    }
    public DescriptorHandle(CpuDescriptorHandle cpuHandle, GpuDescriptorHandle gpuHandle)
    {
      _CPUHandle = cpuHandle;
      _GPUHandle = gpuHandle;
    }

    public CpuDescriptorHandle CPUHandle => _CPUHandle;
    public GpuDescriptorHandle GPUHandle => _GPUHandle;
    public bool IsNull => _CPUHandle.Ptr == -1;
    public bool IsShaderVisible => _GPUHandle.Ptr != -1;

    public static DescriptorHandle operator+ (DescriptorHandle handle, int offsetScaledByDescriptorSize)
    {
      DescriptorHandle ret = new DescriptorHandle(handle._CPUHandle, handle._GPUHandle);
      if(handle._CPUHandle.Ptr != -1) { handle._CPUHandle.Ptr += offsetScaledByDescriptorSize; }
      if(handle._GPUHandle.Ptr != -1) { handle._GPUHandle.Ptr += offsetScaledByDescriptorSize; }
      return handle;
    }
  }

  public class UserDescriptorHeap
  {
    private DescriptorHeap _Heap;
    private DescriptorHeapDescription _HeapDesc;
    private int _DescriptorSize;
    private int _NumFreeDescriptors;
    private DescriptorHandle _FirstHandle;
    private DescriptorHandle _NextHandle;

    public DescriptorHeap Heap => _Heap;

    public bool HasAvailableSpace(int count) => count <= _NumFreeDescriptors;
    public DescriptorHandle HandleAtOffset(int offset) => _FirstHandle + offset * _DescriptorSize;
    public bool ValidateHandle(in DescriptorHandle handle)
    {
      if(handle.CPUHandle.Ptr < _FirstHandle.CPUHandle.Ptr ||
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

    public UserDescriptorHeap(DescriptorHeapType type, int maxCount)
    {
      _HeapDesc.Type = type;
      _HeapDesc.DescriptorCount = maxCount;
      _HeapDesc.Flags = DescriptorHeapFlags.ShaderVisible;
      _HeapDesc.NodeMask = 1;
    }

    public void Create(string debugHeapName)
    {
      _Heap = GraphicsCore.Device.CreateDescriptorHeap(_HeapDesc);
      _Heap.Name = debugHeapName;
      _DescriptorSize = GraphicsCore.Device.GetDescriptorHandleIncrementSize(_HeapDesc.Type);
      _NumFreeDescriptors = _HeapDesc.DescriptorCount;
      _FirstHandle = new DescriptorHandle(_Heap.CPUDescriptorHandleForHeapStart, _Heap.GPUDescriptorHandleForHeapStart);
      _NextHandle = _FirstHandle;
    }

    DescriptorHandle Alloc(int count)
    {
      Debug.Assert(HasAvailableSpace(count), "Descriptor Heap out of space.  Increase heap size.");
      DescriptorHandle ret = _NextHandle;
      _NextHandle += count * _DescriptorSize;
      return ret;
    }
  }
}