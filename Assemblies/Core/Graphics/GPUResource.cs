namespace Core.Graphics
{
  using SharpDX.Direct3D12;
  using System;
  using System.Runtime.InteropServices;

  /// <summary>
  /// Defines the <see cref="GPUResource" />
  /// </summary>
  public class GPUResource
  {
    /// <summary>
    /// Gets the Resource
    /// </summary>
    public Resource Resource => _Resource;

    /// <summary>
    /// Defines the _UsageState
    /// </summary>
    public ResourceStates UsageState {
      get => _UsageState;
      set => _UsageState = value;
    }

    /// <summary>
    /// Defines the _TransitionState
    /// </summary>
    public ResourceStates TransitionState {
      get => _TransitionState;
      set => _TransitionState = value;
    }

    /// <summary>
    /// Gets the GPUVirtualAddress
    /// </summary>
    public long GPUVirtualAddress => _GPUVirtualAddress;

    /// <summary>
    /// Defines the _Resource
    /// </summary>
    protected Resource _Resource;

    /// <summary>
    /// Defines the _UsageState
    /// </summary>
    protected ResourceStates _UsageState = ResourceStates.Common;

    /// <summary>
    /// Defines the _TransitionState
    /// </summary>
    protected ResourceStates _TransitionState = ResourceStates.Common;

    /// <summary>
    /// Defines the _GPUVirtualAddress
    /// </summary>
    protected long _GPUVirtualAddress = 0;

    /// <summary>
    /// Defines the _UserAllocatedMemory
    /// </summary>
    internal IntPtr _UserAllocatedMemory = IntPtr.Zero;

    /// <summary>
    /// Initializes a new instance of the <see cref="GPUResource"/> class.
    /// </summary>
    /// <param name="resource">The <see cref="Resource"/></param>
    /// <param name="usage">The <see cref="ResourceStates"/></param>
    public GPUResource(Resource resource, ResourceStates usage)
    {
      _Resource = resource;
      _UsageState = usage;
    }

    /// <summary>
    /// The Destroy
    /// </summary>
    public virtual void Destroy()
    {
      _Resource?.Dispose();
      _Resource = null;
      _GPUVirtualAddress = 0;

      if (_UserAllocatedMemory != null)
      {
        Marshal.FreeHGlobal(_UserAllocatedMemory);
        _UserAllocatedMemory = IntPtr.Zero;
      }
    }
  }
}
