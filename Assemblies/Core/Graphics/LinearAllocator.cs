﻿namespace Core.Graphics
{
  using SharpDX.Direct3D12;
  using System;
  using System.Collections.Generic;
  using System.Diagnostics;

  #region Enums

  /// <summary>
  /// Defines the ELinearAllocatorType
  /// </summary>
  public enum ELinearAllocatorType
  {
    /// <summary>
    /// Defines the InvalidAllocator
    /// </summary>
    InvalidAllocator = -1,
    /// <summary>
    /// Defines the GpuExclusive
    /// </summary>
    GpuExclusive = 0,
    /// <summary>
    /// Defines the CpuWritable
    /// </summary>
    CpuWritable = 1,
    /// <summary>
    /// Defines the NumAllocatorType
    /// </summary>
    NumAllocatorType
  }

  /// <summary>
  /// Defines the ELinearAllocatorPageSize
  /// </summary>
  public enum ELinearAllocatorPageSize
  {
    /// <summary>
    /// GpuAllocatorPageSize = 64K
    /// </summary>
    GpuAllocatorPageSize = 0x10000,
    /// <summary>
    /// CpuAllocatorPageSize = 2MB
    /// </summary>
    CpuAllocatorPageSize = 0x200000
  }

  #endregion

  /// <summary>
  /// Defines the <see cref="DynAlloc" />
  /// </summary>
  public struct DynAlloc
  {
    #region Fields

    /// <summary>
    /// Defines the Buffer
    /// </summary>
    public GPUResource Buffer;

    /// <summary>
    /// Defines the DataPtr
    /// </summary>
    public IntPtr DataPtr;

    /// <summary>
    /// Defines the GPUVirtualAddress
    /// </summary>
    public long GPUVirtualAddress;

    /// <summary>
    /// Defines the Offset
    /// </summary>
    public long Offset;

    /// <summary>
    /// Defines the Size
    /// </summary>
    public long Size;

    #endregion

    #region Constructors









    public DynAlloc(GPUResource baseResource, long offset, long size, IntPtr dataPtr, long gpuAddress = 0)
    {
      Buffer = baseResource;
      Offset = offset;
      Size = size;
      GPUVirtualAddress = gpuAddress;
      DataPtr = dataPtr;
    }

    #endregion
  }

  /// <summary>
  /// Defines the <see cref="LinearAllocationPage" />
  /// </summary>
  public class LinearAllocationPage : GPUResource, IDisposable
  {
    #region Fields

    /// <summary>
    /// Defines the CPUVirtualAddress
    /// </summary>
    public IntPtr CPUVirtualAddress;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="LinearAllocationPage"/> class.
    /// </summary>
    /// <param name="resource">The <see cref="Resource"/></param>
    /// <param name="usage">The <see cref="ResourceStates"/></param>
    public LinearAllocationPage(Resource resource, ResourceStates usage) : base(resource, usage)
    {
      _GPUVirtualAddress = _Resource.GPUVirtualAddress;
      CPUVirtualAddress = _Resource.Map(0, null);
    }

    #endregion

    #region Methods

    /// <summary>
    /// The Dispose
    /// </summary>
    public void Dispose()
    {
      _Resource.Unmap(0);
      _Resource?.Dispose();
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// The Map
    /// </summary>
    public void Map()
    {
      if (CPUVirtualAddress == IntPtr.Zero) { CPUVirtualAddress = _Resource.Map(0, null); }
    }

    /// <summary>
    /// The Unmap
    /// </summary>
    public void Unmap()
    {
      if (CPUVirtualAddress != IntPtr.Zero)
      {
        _Resource.Unmap(0, null);
        CPUVirtualAddress = IntPtr.Zero;
      }
    }

    #endregion
  }

  /// <summary>
  /// Defines the <see cref="LinearAllocator" />
  /// </summary>
  public class LinearAllocator
  {
    #region Fields

    /// <summary>
    /// Defines the _PageManager
    /// </summary>
    private static LinearAllocatorPageManager[] _PageManager = new LinearAllocatorPageManager[2];

    /// <summary>
    /// Defines the _CurPage
    /// </summary>
    private LinearAllocationPage _CurPage;

    /// <summary>
    /// Defines the _CurrentOffset
    /// </summary>
    private long _CurrentOffset;

    /// <summary>
    /// Defines the _LargePageList
    /// </summary>
    private List<LinearAllocationPage> _LargePageList = new List<LinearAllocationPage>();

    /// <summary>
    /// Defines the _PageSize
    /// </summary>
    private long _PageSize;

    /// <summary>
    /// Defines the _RetiredPages
    /// </summary>
    private List<LinearAllocationPage> _RetiredPages = new List<LinearAllocationPage>();

    /// <summary>
    /// Defines the _Type
    /// </summary>
    private ELinearAllocatorType _Type;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="LinearAllocator"/> class.
    /// </summary>
    /// <param name="type">The <see cref="ELinearAllocatorType"/></param>
    public LinearAllocator(ELinearAllocatorType type)
    {
      Debug.Assert(type > ELinearAllocatorType.InvalidAllocator && type < ELinearAllocatorType.NumAllocatorType);
      _Type = type;
      _CurrentOffset = ~0;
      _PageSize = _Type == ELinearAllocatorType.GpuExclusive ? (long)ELinearAllocatorPageSize.GpuAllocatorPageSize : (long)ELinearAllocatorPageSize.CpuAllocatorPageSize;
    }

    #endregion

    #region Methods

    /// <summary>
    /// The DestroyAll
    /// </summary>
    public static void DestroyAll()
    {
      _PageManager[0].Destroy();
      _PageManager[1].Destroy();
    }

    /// <summary>
    /// The Allocate
    /// </summary>
    /// <param name="sizeInBytes">The <see cref="long"/></param>
    /// <param name="alignement">The <see cref="long"/></param>
    /// <returns>The <see cref="DynAlloc"/></returns>
    public DynAlloc Allocate(long sizeInBytes, long alignement = Constants.DEFAULT_ALIGN)
    {
      var alignementMask = alignement - 1;
      Debug.Assert((alignementMask & alignement) == 0);

      var alignedSize = sizeInBytes.AlignUpWithMask(alignementMask);

      if (alignedSize > _PageSize) { return AllocateLargePage(alignedSize); }

      _CurrentOffset = _CurrentOffset.AlignUpWithMask(alignementMask);

      if (_CurrentOffset + alignedSize > _PageSize)
      {
        Debug.Assert(_CurPage != null);
        _RetiredPages.Add(_CurPage);
        _CurPage = null;
      }

      if (_CurPage == null)
      {
        _CurPage = _PageManager[(int)_Type].RequestPage();
        _CurrentOffset = 0;
      }

      var ret = new DynAlloc(_CurPage, _CurrentOffset, alignedSize, new IntPtr(_CurPage.CPUVirtualAddress.ToInt64() + _CurrentOffset), _CurPage.GPUVirtualAddress + _CurrentOffset);

      _CurrentOffset += alignedSize;

      return ret;
    }

    /// <summary>
    /// The CleanUpPages
    /// </summary>
    /// <param name="fenceValue">The <see cref="long"/></param>
    public void CleanUpPages(long fenceValue)
    {
      if (_CurPage == null) { return; }

      _RetiredPages.Add(_CurPage);
      _CurPage = null;
      _CurrentOffset = 0;
      _PageManager[(int)_Type].DiscardPages(fenceValue, _RetiredPages);
      _RetiredPages.Clear();

      _PageManager[(int)_Type].FreeLargePages(fenceValue, _LargePageList);
      _LargePageList.Clear();
    }

    /// <summary>
    /// The AllocateLargePage
    /// </summary>
    /// <param name="sizeInBytes">The <see cref="long"/></param>
    /// <param name="alignement">The <see cref="long"/></param>
    /// <returns>The <see cref="DynAlloc"/></returns>
    private DynAlloc AllocateLargePage(long sizeInBytes)
    {
      var oneOff = _PageManager[(int)_Type].CreateNewPage(sizeInBytes);
      _LargePageList.Add(oneOff);

      return new DynAlloc(oneOff, 0, sizeInBytes, oneOff.CPUVirtualAddress, oneOff.GPUVirtualAddress);
    }

    #endregion
  }

  /// <summary>
  /// Defines the <see cref="LinearAllocatorPageManager" />
  /// </summary>
  public class LinearAllocatorPageManager
  {
    #region Fields

    /// <summary>
    /// Defines the _AutoType
    /// </summary>
    private static ELinearAllocatorType _AutoType;

    /// <summary>
    /// Defines the _AllocationType
    /// </summary>
    private ELinearAllocatorType _AllocationType;

    /// <summary>
    /// Defines the _AvailablePages
    /// </summary>
    private Queue<LinearAllocationPage> _AvailablePages = new Queue<LinearAllocationPage>();

    /// <summary>
    /// Defines the _DeletionQueue
    /// </summary>
    private Queue<Tuple<long, LinearAllocationPage>> _DeletionQueue = new Queue<Tuple<long, LinearAllocationPage>>();

    /// <summary>
    /// Defines the _Mutex
    /// </summary>
    private object _Mutex = new object();

    /// <summary>
    /// Defines the _PagePool
    /// </summary>
    private List<LinearAllocationPage> _PagePool = new List<LinearAllocationPage>();

    /// <summary>
    /// Defines the _RetiredPages
    /// </summary>
    private Queue<Tuple<long, LinearAllocationPage>> _RetiredPages = new Queue<Tuple<long, LinearAllocationPage>>();

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="LinearAllocatorPageManager"/> class.
    /// </summary>
    public LinearAllocatorPageManager()
    {
      _AllocationType = _AutoType;
      _AutoType = _AutoType + 1;
      Debug.Assert(_AutoType <= ELinearAllocatorType.NumAllocatorType);
    }

    #endregion

    #region Methods

    /// <summary>
    /// The CreateNewPage
    /// </summary>
    /// <param name="pageSize">The <see cref="UInt64"/></param>
    /// <returns>The <see cref="LinearAllocationPage"/></returns>
    public LinearAllocationPage CreateNewPage(long pageSize = 0)
    {
      var heapProp = new HeapProperties
      {
        CPUPageProperty = CpuPageProperty.Unknown,
        MemoryPoolPreference = MemoryPool.Unknown,
        CreationNodeMask = 1,
        VisibleNodeMask = 1
      };

      var resourceDesc = new ResourceDescription
      {
        Dimension = ResourceDimension.Buffer,
        Alignment = 0,
        Height = 1,
        DepthOrArraySize = 1,
        MipLevels = 1,
        Format = SharpDX.DXGI.Format.Unknown,
        SampleDescription = new SharpDX.DXGI.SampleDescription { Count = 1, Quality = 0 },
        Layout = TextureLayout.RowMajor
      };

      ResourceStates defaultUsage;

      if (_AllocationType == ELinearAllocatorType.GpuExclusive)
      {
        heapProp.Type = HeapType.Default;
        resourceDesc.Width = pageSize == 0 ? (long)ELinearAllocatorPageSize.GpuAllocatorPageSize : pageSize;
        resourceDesc.Flags = ResourceFlags.AllowUnorderedAccess;
        defaultUsage = ResourceStates.UnorderedAccess;
      }
      else
      {
        heapProp.Type = HeapType.Upload;
        resourceDesc.Width = pageSize == 0 ? (long)ELinearAllocatorPageSize.CpuAllocatorPageSize : pageSize;
        resourceDesc.Flags = ResourceFlags.None;
        defaultUsage = ResourceStates.GenericRead;
      }

      var buffer = Globals.Device.CreateCommittedResource(heapProp, HeapFlags.None, resourceDesc, defaultUsage, null);
      buffer.Name = "LinearAllocator Page";

      return new LinearAllocationPage(buffer, defaultUsage);
    }

    /// <summary>
    /// The Destroy
    /// </summary>
    public void Destroy()
    {
      _PagePool.ForEach(p => p.Dispose());
      _PagePool.Clear();
    }

    /// <summary>
    /// The DiscardPages
    /// </summary>
    /// <param name="fenceValue">The <see cref="long"/></param>
    /// <param name="usedPages">The <see cref="List{LinearAllocationPage}"/></param>
    public void DiscardPages(long fenceValue, List<LinearAllocationPage> usedPages)
    {
      lock (_Mutex)
      {
        foreach (var page in usedPages)
        {
          _RetiredPages.Enqueue(new Tuple<long, LinearAllocationPage>(fenceValue, page));
        }
      }
    }

    /// <summary>
    /// The FreeLargePages
    /// </summary>
    /// <param name="fenceValue">The <see cref="long"/></param>
    /// <param name="largePages">The <see cref="List{LinearAllocationPage}"/></param>
    public void FreeLargePages(long fenceValue, List<LinearAllocationPage> largePages)
    {
      lock (_Mutex)
      {
        while (_DeletionQueue.Count != 0 && Globals.CommandManager.IsFenceComplete(_DeletionQueue.Peek().Item1))
        {
          (var fence, var page) = _DeletionQueue.Dequeue();
          page.Dispose();
          page = null;
        }

        foreach (var page in largePages)
        {
          page.Unmap();
          _DeletionQueue.Enqueue(new Tuple<long, LinearAllocationPage>(fenceValue, page));
        }
      }
    }

    /// <summary>
    /// The RequestPage
    /// </summary>
    /// <returns>The <see cref="LinearAllocationPage"/></returns>
    public LinearAllocationPage RequestPage()
    {
      lock (_Mutex)
      {
        while (_RetiredPages.Count != 0 && Globals.CommandManager.IsFenceComplete(_RetiredPages.Peek().Item1))
        {
          _AvailablePages.Enqueue(_RetiredPages.Peek().Item2);
          _RetiredPages.Dequeue();
        }

        LinearAllocationPage page = null;

        if (_AvailablePages.Count != 0) { page = _AvailablePages.Dequeue(); }
        else
        {
          page = CreateNewPage();
          _PagePool.Add(page);
        }

        return page;
      }
    }

    #endregion
  }
}
