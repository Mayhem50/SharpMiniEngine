using Core.Graphics;
using SharpDX.Direct3D12;
using System.Collections.Generic;
using System.Diagnostics;

namespace Core
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
}