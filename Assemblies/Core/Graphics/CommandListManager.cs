namespace Core.Graphics
{
  using SharpDX.Direct3D12;
  using System;
  using System.Diagnostics;
  using System.Threading;

  /// <summary>
  /// Defines the <see cref="CCommandQueue" />
  /// </summary>
  public class CCommandQueue : IDisposable
  {
    #region Fields

    /// <summary>
    /// Defines the _Type
    /// </summary>
    private readonly CommandListType _Type;

    /// <summary>
    /// Defines the _AllocatorPool
    /// </summary>
    private CommandAllocatorPool _AllocatorPool;

    /// <summary>
    /// Defines the _commandQueue
    /// </summary>
    private CommandQueue _CommandQueue;

    /// <summary>
    /// Defines the _EventMutex
    /// </summary>
    private object _EventMutex = new object();

    /// <summary>
    /// Defines the _Fence
    /// </summary>
    private Fence _Fence;

    /// <summary>
    /// Defines the _FenceEventHandle
    /// </summary>
    private AutoResetEvent _FenceEventHandle;

    /// <summary>
    /// Defines the _FenceMutex
    /// </summary>
    private object _FenceMutex = new object();

    /// <summary>
    /// Defines the _LastCompletedFenceValue
    /// </summary>
    private long _LastCompletedFenceValue;

    /// <summary>
    /// Defines the _NextFenceValue
    /// </summary>
    private long _NextFenceValue;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CCommandQueue"/> class.
    /// </summary>
    /// <param name="type">The <see cref="CommandListType"/></param>
    public CCommandQueue(CommandListType type)
    {
      _Type = type;
      _CommandQueue = null;
      _Fence = null;
      _NextFenceValue = (long)_Type << 56 | 1;
      _LastCompletedFenceValue = (long)_Type << 56;
      _AllocatorPool = new CommandAllocatorPool(_Type);
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the CommandQueue
    /// </summary>
    public CommandQueue CommandQueue { get => _CommandQueue; private set => _CommandQueue = value; }

    /// <summary>
    /// Gets a value indicating whether IsReady
    /// </summary>
    public bool IsReady => CommandQueue != null;

    /// <summary>
    /// Gets or sets the NextFenceValue
    /// </summary>
    public long NextFenceValue { get => _NextFenceValue; private set => _NextFenceValue = value; }

    #endregion

    #region Methods

    /// <summary>
    /// The Create
    /// </summary>
    /// <param name="device">The <see cref="Device"/></param>
    public void Create(Device device)
    {
      Debug.Assert(device != null);
      Debug.Assert(IsReady);
      Debug.Assert(_AllocatorPool.Size == 0);

      var queueDesc = new CommandQueueDescription
      {
        Type = _Type,
        NodeMask = 1
      };

      _CommandQueue = device.CreateCommandQueue(queueDesc);
      _CommandQueue.Name = "CommandListManager._CommandQueue";

      _Fence = device.CreateFence(0, FenceFlags.None);
      Debug.Assert(_Fence != null);
      _Fence.Name = "CommandListManager._Fence";
      _Fence.Signal((long)_Type << 56);

      _FenceEventHandle = new AutoResetEvent(false);
      Debug.Assert(!_FenceEventHandle.SafeWaitHandle.IsInvalid);

      _AllocatorPool.Create(device);

      Debug.Assert(IsReady);
    }

    /// <summary>
    /// The Dispose
    /// </summary>
    public void Dispose()
    {
      Shutdown();
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// The IncrementFence
    /// </summary>
    /// <returns>The <see cref="long"/></returns>
    public long IncrementFence()
    {
      lock (_FenceMutex)
      {
        _CommandQueue.Signal(_Fence, _NextFenceValue);
        return _NextFenceValue++;
      }
    }

    /// <summary>
    /// The IsFenceComplete
    /// </summary>
    /// <param name="fenceValue">The <see cref="long"/></param>
    /// <returns>The <see cref="bool"/></returns>
    public bool IsFenceComplete(long fenceValue)
    {
      if (fenceValue > _LastCompletedFenceValue)
      {
        _LastCompletedFenceValue = Math.Max(_LastCompletedFenceValue, _Fence.CompletedValue);
      }

      return fenceValue <= _LastCompletedFenceValue;
    }

    /// <summary>
    /// The Shutdown
    /// </summary>
    public void Shutdown()
    {
      if (_CommandQueue == null) { return; }

      _AllocatorPool.Shutdown();
      _FenceEventHandle.Dispose();
      _Fence.Dispose();
      _Fence = null;

      _CommandQueue.Dispose();
      _CommandQueue = null;
    }

    /// <summary>
    /// The StallForFence
    /// </summary>
    /// <param name="fenceValue">The <see cref="long"/></param>
    public void StallForFence(long fenceValue)
    {
      var producer = GraphicsCore.CommandManager.Queue((CommandListType)(fenceValue >> 56));
      _CommandQueue.Wait(producer._Fence, fenceValue);
    }

    /// <summary>
    /// The StallForProducer
    /// </summary>
    /// <param name="producer">The <see cref="CCommandQueue"/></param>
    public void StallForProducer(CCommandQueue producer)
    {
      Debug.Assert(producer._NextFenceValue > 0);
      _CommandQueue.Wait(producer._Fence, producer._NextFenceValue - 1);
    }

    /// <summary>
    /// The WaitForFence
    /// </summary>
    /// <param name="fenceValue">The <see cref="long"/></param>
    public void WaitForFence(long fenceValue)
    {
      if (IsFenceComplete(fenceValue)) { return; }

      lock (_EventMutex)
      {
        _Fence.SetEventOnCompletion(fenceValue, _FenceEventHandle.SafeWaitHandle.DangerousGetHandle());
        _FenceEventHandle.WaitOne();
        _LastCompletedFenceValue = fenceValue;
      }
    }

    /// <summary>
    /// The DiscardAllocator
    /// </summary>
    /// <param name="fenceValueForReset">The <see cref="long"/></param>
    /// <param name="allocator">The <see cref="CommandAllocator"/></param>
    internal void DiscardAllocator(long fenceValueForReset, CommandAllocator allocator)
    {
      _AllocatorPool.DiscardAllocator(fenceValueForReset, allocator);
    }

    /// <summary>
    /// The ExecuteCommandList
    /// </summary>
    /// <param name="list">The <see cref="CommandList"/></param>
    /// <returns>The <see cref="long"/></returns>
    internal long ExecuteCommandList(CommandList list)
    {
      lock (_FenceMutex)
      {
        ((GraphicsCommandList)list).Close();
        _CommandQueue.ExecuteCommandList(list);
        _CommandQueue.Signal(_Fence, _NextFenceValue);
        return _NextFenceValue++;
      }
    }

    /// <summary>
    /// The RequestAllocator
    /// </summary>
    /// <returns>The <see cref="CommandAllocator"/></returns>
    internal CommandAllocator RequestAllocator()
    {
      var completedFence = _Fence.CompletedValue;
      return _AllocatorPool.RequestAllocator(completedFence);
    }

    /// <summary>
    /// The WaitForIdle
    /// </summary>
    internal void WaitForIdle() => WaitForFence(IncrementFence());

    #endregion
  }

  /// <summary>
  /// Defines the <see cref="CommandListManager" />
  /// </summary>
  public class CommandListManager : IDisposable
  {
    #region Fields

    /// <summary>
    /// Defines the _ComputeQueue
    /// </summary>
    private CCommandQueue _ComputeQueue;

    /// <summary>
    /// Defines the _CopyQueue
    /// </summary>
    private CCommandQueue _CopyQueue;

    /// <summary>
    /// Defines the _Device
    /// </summary>
    private Device _Device;

    /// <summary>
    /// Defines the _GraphicsQueue
    /// </summary>
    private CCommandQueue _GraphicsQueue;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandListManager"/> class.
    /// </summary>
    public CommandListManager()
    {
      _GraphicsQueue = new CCommandQueue(CommandListType.Direct);
      _ComputeQueue = new CCommandQueue(CommandListType.Direct);
      _CopyQueue = new CCommandQueue(CommandListType.Direct);
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the CommandQueue
    /// </summary>
    public CommandQueue CommandQueue => _GraphicsQueue.CommandQueue;

    /// <summary>
    /// Gets the ComputeQueue
    /// </summary>
    public CCommandQueue ComputeQueue => _ComputeQueue;

    /// <summary>
    /// Gets the CopyQueue
    /// </summary>
    public CCommandQueue CopyQueue => _CopyQueue;

    /// <summary>
    /// Gets the GraphicsQueue
    /// </summary>
    public CCommandQueue GraphicsQueue => _GraphicsQueue;

    #endregion

    #region Methods

    /// <summary>
    /// The WaitForFence
    /// </summary>
    /// <param name="fenceValue">The <see cref="long"/></param>
    public static void WaitForFence(long fenceValue)
    {
      var producer = GraphicsCore.CommandManager.Queue((CommandListType)(fenceValue >> 56));
      producer.WaitForFence(fenceValue);
    }

    /// <summary>
    /// The Create
    /// </summary>
    /// <param name="device">The <see cref="Device"/></param>
    public void Create(Device device)
    {
      Debug.Assert(device != null);

      _Device = device;
      _GraphicsQueue.Create(device);
      _ComputeQueue.Create(device);
      _CopyQueue.Create(device);
    }

    /// <summary>
    /// The CreateCommandList
    /// </summary>
    /// <param name="type">The <see cref="CommandListType"/></param>
    /// <param name="list">The <see cref="GraphicsCommandList"/></param>
    /// <param name="allocator">The <see cref="CommandAllocator"/></param>
    public void CreateCommandList(CommandListType type, out GraphicsCommandList list, out CommandAllocator allocator)
    {
      Debug.Assert(type != CommandListType.Bundle, "Bundles are not yet supported");

      CommandAllocator tempAllocatorRef = null;

      switch (type)
      {
        case CommandListType.Direct:
          tempAllocatorRef = _GraphicsQueue.RequestAllocator();
          break;
        case CommandListType.Bundle:
          break;
        case CommandListType.Compute:
          tempAllocatorRef = _ComputeQueue.RequestAllocator();
          break;
        case CommandListType.Copy:
          tempAllocatorRef = _CopyQueue.RequestAllocator();
          break;
      }

      list = _Device.CreateCommandList(type, tempAllocatorRef, null);
      list.Name = "CommandList";
      allocator = tempAllocatorRef;
      Debug.Assert(list != null);
    }

    /// <summary>
    /// The Dispose
    /// </summary>
    public void Dispose()
    {
      Shutdown();
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// The IdleGPU
    /// </summary>
    public void IdleGPU()
    {
      _GraphicsQueue.WaitForIdle();
      _ComputeQueue.WaitForIdle();
      _CopyQueue.WaitForIdle();
    }

    /// <summary>
    /// The IsFenceComplete
    /// </summary>
    /// <param name="fenceValue">The <see cref="long"/></param>
    /// <returns>The <see cref="bool"/></returns>
    public bool IsFenceComplete(long fenceValue) => Queue((CommandListType)(fenceValue >> 56)).IsFenceComplete(fenceValue);

    /// <summary>
    /// The Queue
    /// </summary>
    /// <param name="type">The <see cref="CommandListType"/></param>
    /// <returns>The <see cref="CCommandQueue"/></returns>
    public CCommandQueue Queue(CommandListType type = CommandListType.Direct)
    {
      switch (type)
      {
        case CommandListType.Compute: return _ComputeQueue;
        case CommandListType.Copy: return _CopyQueue;
        default: return _GraphicsQueue;
      }
    }

    /// <summary>
    /// The Shutdown
    /// </summary>
    public void Shutdown()
    {
      _GraphicsQueue.Shutdown();
      _ComputeQueue.Shutdown();
      _CopyQueue.Shutdown();
    }

    #endregion
  }
}
