using SharpDX.Direct3D12;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Core.Graphics
{
  public class ContextManager
  {
    public ContextManager()
    {
      for (int idx = 0; idx < 4; idx++)
      {
        _ContextPool[idx] = new List<CommandContext>();
        _AvailableContexts[idx] = new Queue<CommandContext>();
      }
    }
    public CommandContext AllocateContext(CommandListType type)
    {
      lock (_ContextAllocationMutex)
      {
        var availableContexts = _AvailableContexts[(int)type];
        CommandContext ret = null;

        if(availableContexts.Count == 0)
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
    public void FreeContext(CommandContext context)
    {
      Debug.Assert(context != null);

    }
    public void DestroyAllContexts()
    {

    }

    private List<List<CommandContext>> _ContextPool = new List<List<CommandContext>>(4);
    private List<Queue<CommandContext>> _AvailableContexts = new List<Queue<CommandContext>>(4);
    private object _ContextAllocationMutex = new object();
  }

  public class CommandContext
  {
    public CommandContext(CommandListType type)
    {
      Type = type;
    }

    public CommandListType Type { get; internal set; }

    internal void Initialize()
    {
      throw new NotImplementedException();
    }

    internal void Reset()
    {
      throw new NotImplementedException();
    }
  }
}
