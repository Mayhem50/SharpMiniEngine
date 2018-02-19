using SharpDX;
using SharpDX.Direct3D12;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Core.Graphics
{
  public class DynamicDescriptorHeap : IDisposable
  {
    private static uint _NumDescriptorsPerHeap = 1024;
    private static object _Mutex = new object();
    private static List<List<DescriptorHeap>> _DescriptorHeapPool = new List<List<DescriptorHeap>>(2);
    private static List<Queue<Tuple<UInt64, DescriptorHeap>>> _RetiredDescriptorHeaps = new List<Queue<Tuple<ulong, DescriptorHeap>>>(2);
    private static List<Queue<DescriptorHeap>> _AvailableDescriptorHeaps = new List<Queue<DescriptorHeap>>(2);

    private CommandContext _OwningContext;
    private DescriptorHeap _CurrentHeap;
    private readonly DescriptorHeapType _DescriptorType;
    private uint _DescriptorSize;
    private uint _CurrentOffset;
    private DescriptorHandle _FirstDescriptor;
    private List<DescriptorHeap> _RetiredHeaps = new List<DescriptorHeap>();

    internal class DescriptorTableCache
    {
      public uint AssignedHandleBitMap;
      public CpuDescriptorHandle[] TableStart;
      public uint TableSize;
    }

    internal class DescriptorHandleCache
    {
      public delegate void CommandListSetFunc(int rootParameterIndex, GpuDescriptorHandle baseDescriptor);
      public DescriptorHandleCache() { ClearCache(); }

      public void ClearCache()
      {
        throw new NotImplementedException();
      }

      public int RootDescriptorTablesBitMap;
      public uint StaleRootParamsBitMap;
      public int MaxCachedDescriptors;

      public static readonly int MaxNumDescriptors = 256;
      public static readonly int MaxNumDescriptorTables = 16;

      DescriptorTableCache[] RootDescriptorTable = new DescriptorTableCache[MaxNumDescriptorTables];
      CpuDescriptorHandle[] HandleCache = new CpuDescriptorHandle[MaxNumDescriptors];

      public int StagedSize {
        get {
          var needSpace = 0;
          uint rootIndex = 0;
          var staleParams = StaleRootParamsBitMap;

          while ((rootIndex = (uint)BitScanner.BitScanForward(staleParams)) != 0)
          {
            staleParams ^= (uint)(1 << (int)rootIndex);

            var maxSetHandle = BitScanner.BitScanReverse(RootDescriptorTable[rootIndex].AssignedHandleBitMap);
            Debug.Assert(maxSetHandle != 0, "Root entry marked as stale but has no stale descriptors");

            needSpace += maxSetHandle + 1;
          }

          return needSpace;
        }
      }
      void CopyAndBindStaleTables(DescriptorHeapType type, uint descriptorSize, DescriptorHandle destHandleStart, GraphicsCommandList cmdList, CommandListSetFunc setFunction)
      {
        uint staleParamCount = 0;
        var tableSize = new uint[MaxNumDescriptorTables];
        var rootIndices = new uint[MaxNumDescriptorTables];
        var needSpace = 0;
        uint rootIndex;

        var staleParams = StaleRootParamsBitMap;

        while ((rootIndex = (uint)BitScanner.BitScanForward(staleParams)) != 0)
        {
          rootIndices[staleParamCount] = rootIndex;
          staleParams ^= (uint)(1 << (int)rootIndex);

          var maxSetHandle = BitScanner.BitScanReverse(RootDescriptorTable[rootIndex].AssignedHandleBitMap);
          Debug.Assert(maxSetHandle != 0, "Root entry marked as stale but has no stale descriptors");

          needSpace += maxSetHandle + 1;
          tableSize[staleParamCount] = (uint)maxSetHandle + 1;

          staleParamCount++;
        }

        Debug.Assert(staleParamCount <= MaxNumDescriptorTables, "We're only equipped to handle so many descriptor tables");

        StaleRootParamsBitMap = 0;

        const uint maxdescriptorPerCopy = 16;
        var numDestDescriptorRanges = 0;
        var destDescriptorRangeStarts = new CpuDescriptorHandle[maxdescriptorPerCopy];
        var destDescriptorRangeSizes = new int[maxdescriptorPerCopy];

        var numSrcDescriptorRanges = 0;
        var srcDescriptorRangeStarts = new CpuDescriptorHandle[maxdescriptorPerCopy];
        var srcDescriptorRangeSizes = new int[maxdescriptorPerCopy];

        for (uint idx = 0; idx < staleParamCount; idx++)
        {
          rootIndex = rootIndices[idx];
          setFunction?.Invoke((int)rootIndex, destHandleStart.GPUHandle);

          var rootDescTable = RootDescriptorTable[rootIndex];

          var srcHandles = Marshal.UnsafeAddrOfPinnedArrayElement(rootDescTable.TableStart, 0);
          UInt64 setHandles = rootDescTable.AssignedHandleBitMap;
          var curDest = destHandleStart.CPUHandle;
          destHandleStart += (int)(tableSize[idx] * descriptorSize);

          int skipCount;

          while((skipCount = BitScanner.BitScanForward64(setHandles)) != 0)
          {
            setHandles >>= skipCount;
            srcHandles += skipCount;
            curDest.Ptr += skipCount * descriptorSize;

            var descriptorCount = BitScanner.BitScanForward64(~setHandles);
            setHandles >>= descriptorCount;

            if(numSrcDescriptorRanges + descriptorCount > maxdescriptorPerCopy)
            {
              GraphicsCore.Device.CopyDescriptors(numDestDescriptorRanges, destDescriptorRangeStarts, destDescriptorRangeSizes, numSrcDescriptorRanges, srcDescriptorRangeStarts, srcDescriptorRangeSizes, type);

              numSrcDescriptorRanges = 0;
              numDestDescriptorRanges = 0;
            }

            destDescriptorRangeStarts[numDestDescriptorRanges] = curDest;
            destDescriptorRangeSizes[numDestDescriptorRanges] = descriptorCount;
            numDestDescriptorRanges++;

            for(int jdx = 0; jdx < descriptorCount; jdx++)
            {
              srcDescriptorRangeStarts[numSrcDescriptorRanges] = rootDescTable.TableStart[jdx];
              srcDescriptorRangeSizes[numSrcDescriptorRanges] = 1;
              numSrcDescriptorRanges++;
            }

            srcHandles += descriptorCount;
            curDest.Ptr += descriptorCount;
          }
        }

        GraphicsCore.Device.CopyDescriptors(numDestDescriptorRanges, destDescriptorRangeStarts, destDescriptorRangeSizes, numSrcDescriptorRanges, srcDescriptorRangeStarts, srcDescriptorRangeSizes, type);
      }

      void UnbindAllValid()
      {
        StaleRootParamsBitMap = 0;
        var tableParams = (uint)RootDescriptorTablesBitMap;
        var rootIndex = 0;

        while ((rootIndex = BitScanner.BitScanForward(tableParams)) != 0)
        {
          tableParams ^= (uint)(1 << rootIndex);
          if(RootDescriptorTable[rootIndex].AssignedHandleBitMap != 0)
          {
            StaleRootParamsBitMap |= (uint)(1 << rootIndex);
          }
        }
      }
      void StageDescriptorHandles(uint rootIndex, uint offset, uint numHandles, params CpuDescriptorHandle[] handles)
      {
        Debug.Assert(((uint)(1 << (int)rootIndex) & RootDescriptorTablesBitMap) != 0, "Root parameter is not a CBV_SRV_UAV descriptor table");
        Debug.Assert(offset + numHandles <= RootDescriptorTable[rootIndex].TableSize);

        var tableCache = RootDescriptorTable[rootIndex];
        var copyDest = Marshal.UnsafeAddrOfPinnedArrayElement(tableCache.TableStart, (int)offset);

        for(int idx = 0; idx < numHandles; idx++)
        {
          tableCache.TableStart[idx] = handles[idx];
        }

        tableCache.AssignedHandleBitMap |= ((uint)(1 << (int)numHandles) - 1) << (int)offset;
        StaleRootParamsBitMap |= (uint)(1 << (int)rootIndex);
      }
      void ParseRootSignature(DescriptorHeapType type, RootSignature rootSig)
      {

      }
    }
    
    public DynamicDescriptorHeap(CommandContext owningContext, DescriptorHeapType type)
    {
      for(int idx = 0; idx < 2; idx++)
      {
        _DescriptorHeapPool[idx] = new List<DescriptorHeap>();
        _RetiredDescriptorHeaps[idx] = new Queue<Tuple<ulong, DescriptorHeap>>();
        _AvailableDescriptorHeaps[idx] = new Queue<DescriptorHeap>();
      }
    }

    public static void DestroyAll()
    {

    }

    public void Dispose()
    {

    }
  }
}
