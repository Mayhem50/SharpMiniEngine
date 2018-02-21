namespace Core.Graphics
{
  using SharpDX.Direct3D12;
  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Runtime.InteropServices;

  /// <summary>
  /// Defines the <see cref="DynamicDescriptorHeap" />
  /// </summary>
  public class DynamicDescriptorHeap : IDisposable
  {
    /// <summary>
    /// Defines the _NumDescriptorsPerHeap
    /// </summary>
    private static uint _NumDescriptorsPerHeap = 1024;

    /// <summary>
    /// Defines the _Mutex
    /// </summary>
    private static object _Mutex = new object();

    /// <summary>
    /// Defines the _DescriptorHeapPool
    /// </summary>
    private static List<List<DescriptorHeap>> _DescriptorHeapPool = new List<List<DescriptorHeap>>(2);

    /// <summary>
    /// Defines the _RetiredDescriptorHeaps
    /// </summary>
    private static List<Queue<Tuple<UInt64, DescriptorHeap>>> _RetiredDescriptorHeaps = new List<Queue<Tuple<ulong, DescriptorHeap>>>(2);

    /// <summary>
    /// Defines the _AvailableDescriptorHeaps
    /// </summary>
    private static List<Queue<DescriptorHeap>> _AvailableDescriptorHeaps = new List<Queue<DescriptorHeap>>(2);

    /// <summary>
    /// Defines the _OwningContext
    /// </summary>
    private CommandContext _OwningContext;

    /// <summary>
    /// Defines the _CurrentHeap
    /// </summary>
    private DescriptorHeap _CurrentHeap;

    /// <summary>
    /// Defines the _DescriptorType
    /// </summary>
    private readonly DescriptorHeapType _DescriptorType;

    /// <summary>
    /// Defines the _DescriptorSize
    /// </summary>
    private uint _DescriptorSize;

    /// <summary>
    /// Defines the _CurrentOffset
    /// </summary>
    private uint _CurrentOffset;

    /// <summary>
    /// Defines the _FirstDescriptor
    /// </summary>
    private DescriptorHandle _FirstDescriptor;

    /// <summary>
    /// Defines the _RetiredHeaps
    /// </summary>
    private List<DescriptorHeap> _RetiredHeaps = new List<DescriptorHeap>();

    /// <summary>
    /// Defines the _GraphicsHandleCache
    /// </summary>
    private DescriptorHandleCache _GraphicsHandleCache;

    /// <summary>
    /// Defines the _ComputeHandleCache
    /// </summary>
    private DescriptorHandleCache _ComputeHandleCache;

    /// <summary>
    /// The HasSpace
    /// </summary>
    /// <param name="count">The <see cref="uint"/></param>
    /// <returns>The <see cref="bool"/></returns>
    public bool HasSpace(uint count) => _CurrentHeap != null && _CurrentOffset + count <= _NumDescriptorsPerHeap;

    /// <summary>
    /// Defines the <see cref="DescriptorTableCache" />
    /// </summary>
    internal class DescriptorTableCache
    {
      /// <summary>
      /// Defines the AssignedHandleBitMap
      /// </summary>
      public uint AssignedHandleBitMap;

      /// <summary>
      /// Defines the TableStart
      /// </summary>
      public IntPtr TableStart;

      /// <summary>
      /// Defines the TableSize
      /// </summary>
      public uint TableSize;
    }

    /// <summary>
    /// Defines the <see cref="DescriptorHandleCache" />
    /// </summary>
    internal class DescriptorHandleCache
    {
      /// <summary>
      /// The CommandListSetFunc
      /// </summary>
      /// <param name="rootParameterIndex">The <see cref="int"/></param>
      /// <param name="baseDescriptor">The <see cref="GpuDescriptorHandle"/></param>
      public delegate void CommandListSetFunc(int rootParameterIndex, GpuDescriptorHandle baseDescriptor);

      /// <summary>
      /// Initializes a new instance of the <see cref="DescriptorHandleCache"/> class.
      /// </summary>
      public DescriptorHandleCache()
      {
        ClearCache();
      }

      /// <summary>
      /// The ClearCache
      /// </summary>
      public void ClearCache()
      {
        RootDescriptorTablesBitMap = 0;
        MaxCachedDescriptors = 0;
      }

      /// <summary>
      /// Defines the RootDescriptorTablesBitMap
      /// </summary>
      public uint RootDescriptorTablesBitMap;

      /// <summary>
      /// Defines the StaleRootParamsBitMap
      /// </summary>
      public uint StaleRootParamsBitMap;

      /// <summary>
      /// Defines the MaxCachedDescriptors
      /// </summary>
      public uint MaxCachedDescriptors;

      /// <summary>
      /// Defines the MaxNumDescriptors
      /// </summary>
      public static readonly int MaxNumDescriptors = 256;

      /// <summary>
      /// Defines the MaxNumDescriptorTables
      /// </summary>
      public static readonly int MaxNumDescriptorTables = 16;

      /// <summary>
      /// Defines the RootDescriptorTable
      /// </summary>
      public DescriptorTableCache[] RootDescriptorTable = new DescriptorTableCache[MaxNumDescriptorTables];

      /// <summary>
      /// Defines the HandleCache
      /// </summary>
      public CpuDescriptorHandle[] HandleCache = new CpuDescriptorHandle[MaxNumDescriptors];

      /// <summary>
      /// Gets the StagedSize
      /// </summary>
      public int StagedSize {
        get {
          var needSpace = 0;
          var staleParams = StaleRootParamsBitMap;

          while (BitScanner.BitScanForward(staleParams, out var rootIndex))
          {
            staleParams ^= (uint)(1 << (int)rootIndex);
            
            Debug.Assert(BitScanner.BitScanReverse(RootDescriptorTable[rootIndex].AssignedHandleBitMap, out var maxSetHandle), "Root entry marked as stale but has no stale descriptors");

            needSpace += maxSetHandle + 1;
          }

          return needSpace;
        }
      }

      /// <summary>
      /// The CopyAndBindStaleTables
      /// </summary>
      /// <param name="type">The <see cref="DescriptorHeapType"/></param>
      /// <param name="descriptorSize">The <see cref="uint"/></param>
      /// <param name="destHandleStart">The <see cref="DescriptorHandle"/></param>
      /// <param name="setFunction">The <see cref="CommandListSetFunc"/></param>
      public void CopyAndBindStaleTables(DescriptorHeapType type, uint descriptorSize, DescriptorHandle destHandleStart, CommandListSetFunc setFunction)
      {
        uint staleParamCount = 0;
        var tableSize = new int[MaxNumDescriptorTables];
        var rootIndices = new int[MaxNumDescriptorTables];
        var needSpace = 0;
        int rootIndex;

        var staleParams = StaleRootParamsBitMap;

        while (BitScanner.BitScanForward(staleParams, out rootIndex))
        {
          rootIndices[staleParamCount] = rootIndex;
          staleParams ^= (uint)(1 << (int)rootIndex);
          
          Debug.Assert(BitScanner.BitScanReverse(RootDescriptorTable[rootIndex].AssignedHandleBitMap, out var maxSetHandle), "Root entry marked as stale but has no stale descriptors");

          needSpace += maxSetHandle + 1;
          tableSize[staleParamCount] = maxSetHandle + 1;

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

          var srcHandles = rootDescTable.TableStart;
          UInt64 setHandles = rootDescTable.AssignedHandleBitMap;
          var curDest = destHandleStart.CPUHandle;
          destHandleStart += (int)(tableSize[idx] * descriptorSize);
          

          while (BitScanner.BitScanForward64(setHandles, out var skipCount))
          {
            setHandles >>= skipCount;
            srcHandles += skipCount;
            curDest.Ptr += skipCount * descriptorSize;

            BitScanner.BitScanForward64(~setHandles, out var descriptorCount);
            setHandles >>= descriptorCount;

            if (numSrcDescriptorRanges + descriptorCount > maxdescriptorPerCopy)
            {
              Globals.Device.CopyDescriptors(numDestDescriptorRanges, destDescriptorRangeStarts, destDescriptorRangeSizes, numSrcDescriptorRanges, srcDescriptorRangeStarts, srcDescriptorRangeSizes, type);

              numSrcDescriptorRanges = 0;
              numDestDescriptorRanges = 0;
            }

            destDescriptorRangeStarts[numDestDescriptorRanges] = curDest;
            destDescriptorRangeSizes[numDestDescriptorRanges] = descriptorCount;
            numDestDescriptorRanges++;

            for (int jdx = 0; jdx < descriptorCount; jdx++)
            {
              srcDescriptorRangeStarts[numSrcDescriptorRanges] = Marshal.PtrToStructure<CpuDescriptorHandle>(rootDescTable.TableStart + jdx * Marshal.SizeOf<CpuDescriptorHandle>());
              srcDescriptorRangeSizes[numSrcDescriptorRanges] = 1;
              numSrcDescriptorRanges++;
            }

            srcHandles += descriptorCount;
            curDest.Ptr += descriptorCount;
          }
        }

        Globals.Device.CopyDescriptors(numDestDescriptorRanges, destDescriptorRangeStarts, destDescriptorRangeSizes, numSrcDescriptorRanges, srcDescriptorRangeStarts, srcDescriptorRangeSizes, type);
      }

      /// <summary>
      /// The UnbindAllValid
      /// </summary>
      public void UnbindAllValid()
      {
        StaleRootParamsBitMap = 0;
        var tableParams = (uint)RootDescriptorTablesBitMap;

        while (BitScanner.BitScanForward(tableParams, out var rootIndex))
        {
          tableParams ^= (uint)(1 << rootIndex);
          if (RootDescriptorTable[rootIndex].AssignedHandleBitMap != 0)
          {
            StaleRootParamsBitMap |= (uint)(1 << rootIndex);
          }
        }
      }

      /// <summary>
      /// The StageDescriptorHandles
      /// </summary>
      /// <param name="rootIndex">The <see cref="uint"/></param>
      /// <param name="offset">The <see cref="uint"/></param>
      /// <param name="handles">The <see cref="CpuDescriptorHandle[]"/></param>
      public void StageDescriptorHandles(uint rootIndex, uint offset, in CpuDescriptorHandle[] handles)
      {
        Debug.Assert(((uint)(1 << (int)rootIndex) & RootDescriptorTablesBitMap) != 0, "Root parameter is not a CBV_SRV_UAV descriptor table");
        Debug.Assert(offset + handles.Length <= RootDescriptorTable[rootIndex].TableSize);

        var tableCache = RootDescriptorTable[rootIndex];
        var copyDest = tableCache.TableStart + (int)offset;

        for (int idx = 0; idx < handles.Length; idx++)
        {
          var ptr = Marshal.UnsafeAddrOfPinnedArrayElement(handles, idx);
          Marshal.WriteIntPtr(tableCache.TableStart + idx * Marshal.SizeOf<CpuDescriptorHandle>(), ptr);
        }

        tableCache.AssignedHandleBitMap |= ((uint)(1 << (int)handles.Length) - 1) << (int)offset;
        StaleRootParamsBitMap |= (uint)(1 << (int)rootIndex);
      }

      /// <summary>
      /// The ParseRootSignature
      /// </summary>
      /// <param name="type">The <see cref="DescriptorHeapType"/></param>
      /// <param name="rootSig">The <see cref="CRootSignature"/></param>
      public void ParseRootSignature(DescriptorHeapType type, CRootSignature rootSig)
      {
        uint currentOffset = 0;

        Debug.Assert(rootSig.ParametersCount <= 16, "Maybe we need to support something greater");
        StaleRootParamsBitMap = 0;
        RootDescriptorTablesBitMap = (type == DescriptorHeapType.Sampler ? rootSig.SamplerTableBitMap.ToUInt() : rootSig.DescriptorTableBitMap.ToUInt());

        var tableParams = RootDescriptorTablesBitMap;

        while (BitScanner.BitScanForward(tableParams, out var rootIndex))
        {
          tableParams ^= (uint)(1 << rootIndex);

          var tableSize = rootSig.DescriptorTableSize[rootIndex];
          Debug.Assert(tableSize > 0);

          ref var rootDescriptorTable = ref RootDescriptorTable[rootIndex];
          rootDescriptorTable.AssignedHandleBitMap = 0;
          rootDescriptorTable.TableStart = Marshal.UnsafeAddrOfPinnedArrayElement(HandleCache, 0) + (int)currentOffset;
          rootDescriptorTable.TableSize = tableSize;

          currentOffset += tableSize;
        }

        MaxCachedDescriptors = currentOffset;

        Debug.Assert(MaxCachedDescriptors <= MaxNumDescriptors, "Exceeded user-supplied maximum cache size");
      }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicDescriptorHeap"/> class.
    /// </summary>
    /// <param name="owningContext">The <see cref="CommandContext"/></param>
    /// <param name="type">The <see cref="DescriptorHeapType"/></param>
    public DynamicDescriptorHeap(CommandContext owningContext, DescriptorHeapType type)
    {
      _OwningContext = owningContext;
      _DescriptorType = type;
      _CurrentHeap = null;
      _DescriptorSize = (uint)Globals.Device.GetDescriptorHandleIncrementSize(type);
      for (int idx = 0; idx < 2; idx++)
      {
        _DescriptorHeapPool[idx] = new List<DescriptorHeap>();
        _RetiredDescriptorHeaps[idx] = new Queue<Tuple<ulong, DescriptorHeap>>();
        _AvailableDescriptorHeaps[idx] = new Queue<DescriptorHeap>();
      }
    }

    /// <summary>
    /// The DestroyAll
    /// </summary>
    public static void DestroyAll()
    {
      _DescriptorHeapPool.ForEach(p =>
      {
        p.ForEach(d => d?.Dispose());
        p.Clear();
      });
    }

    /// <summary>
    /// The CleanupUsedHeaps
    /// </summary>
    /// <param name="fenceValue">The <see cref="long"/></param>
    public void CleanupUsedHeaps(long fenceValue)
    {
      RetireCurrentHeap();
      RetireUsedHeap(fenceValue);
      _GraphicsHandleCache.ClearCache();
      _ComputeHandleCache.ClearCache();
    }

    /// <summary>
    /// The SetGraphicsDescriptorHandles
    /// </summary>
    /// <param name="rootIndex">The <see cref="uint"/></param>
    /// <param name="offset">The <see cref="uint"/></param>
    /// <param name="handles">The <see cref="CpuDescriptorHandle[]"/></param>
    public void SetGraphicsDescriptorHandles(uint rootIndex, uint offset, in CpuDescriptorHandle[] handles)
    {
      _GraphicsHandleCache.StageDescriptorHandles(rootIndex, offset, handles);
    }

    /// <summary>
    /// The SetComputeDescriptorHandles
    /// </summary>
    /// <param name="rootIndex">The <see cref="uint"/></param>
    /// <param name="offset">The <see cref="uint"/></param>
    /// <param name="handles">The <see cref="CpuDescriptorHandle[]"/></param>
    public void SetComputeDescriptorHandles(uint rootIndex, uint offset, in CpuDescriptorHandle[] handles)
    {
      _ComputeHandleCache.StageDescriptorHandles(rootIndex, offset, handles);
    }

    /// <summary>
    /// The UploadDirect
    /// </summary>
    /// <param name="handle">The <see cref="CpuDescriptorHandle"/></param>
    /// <returns>The <see cref="GpuDescriptorHandle"/></returns>
    public GpuDescriptorHandle UploadDirect(CpuDescriptorHandle handle)
    {
      if (!HasSpace(1))
      {
        RetireCurrentHeap();
        UnbindAllValid();
      }

      _OwningContext.
        }

    /// <summary>
    /// The Dispose
    /// </summary>
    public void Dispose()
    {
    }
  }
}
