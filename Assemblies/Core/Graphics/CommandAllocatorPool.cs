namespace Core.Graphics
{
  using SharpDX.Direct3D12;
  using System;
  using System.Collections.Generic;

  /// <summary>
  /// Defines the <see cref="CommandAllocatorPool" />
  /// </summary>
  public class CommandAllocatorPool : IDisposable
  {
    #region Fields

    /// <summary>
    /// Defines the _Type
    /// </summary>
    private readonly CommandListType _Type;

    /// <summary>
    /// Defines the _AllocatorPool
    /// </summary>
    private List<CommandAllocator> _AllocatorPool = new List<CommandAllocator>();

    /// <summary>
    /// Defines the _Device
    /// </summary>
    private Device _Device;

    /// <summary>
    /// Defines the _Mutex
    /// </summary>
    private object _Mutex = new object();

    /// <summary>
    /// Defines the _ReadyAllocator
    /// </summary>
    private Queue<Tuple<long, CommandAllocator>> _ReadyAllocator = new Queue<Tuple<long, CommandAllocator>>();

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandAllocatorPool"/> class.
    /// </summary>
    /// <param name="type">The <see cref="CommandListType"/></param>
    public CommandAllocatorPool(CommandListType type)
    {
      _Type = type;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the Size
    /// </summary>
    public long Size => _AllocatorPool.Count;

    #endregion

    #region Methods

    /// <summary>
    /// The Create
    /// </summary>
    /// <param name="device">The <see cref="Device"/></param>
    public void Create(Device device)
    {
      _Device = device;
    }

    /// <summary>
    /// The DiscardAllocator
    /// </summary>
    /// <param name="fenceValue">The <see cref="long"/></param>
    /// <param name="allocator">The <see cref="CommandAllocator"/></param>
    public void DiscardAllocator(long fenceValue, CommandAllocator allocator)
    {
      lock (_Mutex)
      {
        _ReadyAllocator.Enqueue(new Tuple<long, CommandAllocator>(fenceValue, allocator));
      }
    }

    /// <summary>
    /// The Dispose
    /// </summary>
    public void Dispose()
    {
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// The RequestAllocator
    /// </summary>
    /// <param name="completedFenceValue">The <see cref="long"/></param>
    /// <returns>The <see cref="CommandAllocator"/></returns>
    public CommandAllocator RequestAllocator(long completedFenceValue)
    {
      lock (_Mutex)
      {
        CommandAllocator result = null;

        if (_ReadyAllocator.Count != 0)
        {
          (var fenceValue, var allocator) = _ReadyAllocator.Peek();

          if (fenceValue <= completedFenceValue)
          {
            result = allocator;
            result.Reset();
            _ReadyAllocator.Dequeue();
          }
        }

        if (result == null)
        {
          result = _Device.CreateCommandAllocator(_Type);
          result.Name = $"CommandAllocator {_AllocatorPool.Count}";
          _AllocatorPool.Add(result);
        }

        return result;
      }
    }

    /// <summary>
    /// The Shutdown
    /// </summary>
    public void Shutdown()
    {
      _AllocatorPool.ForEach(a => a.Dispose());
      _AllocatorPool.Clear();
    }

    #endregion
  }
}
