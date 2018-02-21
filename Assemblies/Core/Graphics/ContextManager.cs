namespace Core.Graphics
{
  using SharpDX.Direct3D12;
  using System.Collections.Generic;
  using System.Diagnostics;

  /// <summary>
  /// Defines the <see cref="ContextManager" />
  /// </summary>
  public class ContextManager
  {
    #region Fields

    /// <summary>
    /// Defines the _AvailableContexts
    /// </summary>
    private List<Queue<CommandContext>> _AvailableContexts = new List<Queue<CommandContext>>(4);

    /// <summary>
    /// Defines the _ContextAllocationMutex
    /// </summary>
    private object _ContextAllocationMutex = new object();

    /// <summary>
    /// Defines the _ContextPool
    /// </summary>
    private List<List<CommandContext>> _ContextPool = new List<List<CommandContext>>(4);

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextManager"/> class.
    /// </summary>
    public ContextManager()
    {
      for (int idx = 0; idx < 4; idx++)
      {
        _ContextPool[idx] = new List<CommandContext>();
        _AvailableContexts[idx] = new Queue<CommandContext>();
      }
    }

    #endregion

    #region Methods

    /// <summary>
    /// The AllocateContext
    /// </summary>
    /// <param name="type">The <see cref="CommandListType"/></param>
    /// <returns>The <see cref="CommandContext"/></returns>
    public CommandContext AllocateContext(CommandListType type)
    {
      lock (_ContextAllocationMutex)
      {
        var availableContexts = _AvailableContexts[(int)type];
        CommandContext ret = null;

        if (availableContexts.Count == 0)
        {
          ret = new CommandContext(type);
          _ContextPool[(int)type].Add(ret);
          ret.Initialize();
        }
        else
        {
          ret = availableContexts.Peek();
          availableContexts.Dequeue();
          ret.Reset();
        }

        Debug.Assert(ret != null);
        Debug.Assert(ret._Type == type);

        return ret;
      }
    }

    /// <summary>
    /// The DestroyAllContexts
    /// </summary>
    public void DestroyAllContexts()
    {
      _ContextPool.ForEach(c => c.Clear());
    }

    /// <summary>
    /// The FreeContext
    /// </summary>
    /// <param name="context">The <see cref="CommandContext"/></param>
    public void FreeContext(CommandContext context)
    {
      Debug.Assert(context != null);
      lock (_ContextAllocationMutex)
      {
        _AvailableContexts[(int)context._Type].Enqueue(context);
      }
    }

    #endregion
  }
}
