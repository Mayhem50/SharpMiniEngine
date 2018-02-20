namespace Core.Graphics
{
  using SharpDX.Direct3D12;
  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
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
    internal float Float;

    /// <summary>
    /// Defines the Int
    /// </summary>
    [FieldOffset(0)]
    internal int Int;

    /// <summary>
    /// Defines the Uint
    /// </summary>
    [FieldOffset(0)]
    internal uint Uint;

    #endregion

    public static implicit operator DWParam(float f) => new DWParam { Float = f };
    public static implicit operator DWParam(uint u) => new DWParam { Uint = u };
    public static implicit operator DWParam(int i) => new DWParam { Int = i };
  }

  /// <summary>
  /// Defines the <see cref="CommandContext" />
  /// </summary>
  public class CommandContext
  {
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandContext"/> class.
    /// </summary>
    /// <param name="type">The <see cref="CommandListType"/></param>
    public CommandContext(CommandListType type)
    {
      Type = type;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the Type
    /// </summary>
    public CommandListType Type { get; internal set; }

    #endregion

    #region Methods

    /// <summary>
    /// The Initialize
    /// </summary>
    internal void Initialize()
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// The Reset
    /// </summary>
    internal void Reset()
    {
      throw new NotImplementedException();
    }

    #endregion
  }

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
        Debug.Assert(ret.Type == type);

        return ret;
      }
    }

    /// <summary>
    /// The DestroyAllContexts
    /// </summary>
    public void DestroyAllContexts()
    {
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
        _AvailableContexts[(int)context.Type].Enqueue(context);
      }
    }

    #endregion
  }
}
