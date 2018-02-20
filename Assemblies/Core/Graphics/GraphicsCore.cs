namespace Core.Graphics
{
  using SharpDX.Direct3D;
  using SharpDX.Direct3D12;
  using System;

  /// <summary>
  /// Defines the <see cref="GraphicsCore" />
  /// </summary>
  public static class GraphicsCore
  {
#if DEBUG
    static Guid WKPDID_D3DDebugObjectName = new Guid(0x429b8c22, 0x9188, 0x4b0c, 0x87, 0x42, 0xac, 0xb0, 0xbf, 0x85, 0xc2, 0x00);
#endif
    /// <summary>
    /// The Initialize
    /// </summary>
    public static void Initialize()
    {
    }

    /// <summary>
    /// The Resize
    /// </summary>
    /// <param name="width">The <see cref="uint"/></param>
    /// <param name="height">The <see cref="uint"/></param>
    public static void Resize(uint width, uint height)
    {
    }

    /// <summary>
    /// The Terminate
    /// </summary>
    public static void Terminate()
    {
    }

    /// <summary>
    /// The Shutdown
    /// </summary>
    public static void Shutdown()
    {
    }

    /// <summary>
    /// The Present
    /// </summary>
    public static void Present()
    {
    }

    /// <summary>
    /// Gets the FrameCount
    /// </summary>
    public static UInt64 FrameCount => _FrameIndex;

    /// <summary>
    /// Gets the FrameTime
    /// </summary>
    public static float FrameTime => _FrameTime;

    /// <summary>
    /// Gets the FrameRate
    /// </summary>
    public static float FrameRate => _FrameTime == 0.0f ? 0.0f : 1.0f / _FrameTime;

    /// <summary>
    /// Defines the DisplayWidth
    /// </summary>
    public static uint DisplayWidth;

    /// <summary>
    /// Defines the DisplayHeight
    /// </summary>
    public static uint DisplayHeight;

    /// <summary>
    /// Defines the Device
    /// </summary>
    public static Device Device;

    /// <summary>
    /// Defines the CommandManager
    /// </summary>
    public static CommandListManager CommandManager;

    /// <summary>
    /// Defines the ContextManager
    /// </summary>
    public static ContextManager ContextManager;

    /// <summary>
    /// Defines the FeatureLevel
    /// </summary>
    public static FeatureLevel FeatureLevel;

    /// <summary>
    /// Defines the TypedUAVLoadSupport_R11G11B10_FLOAT
    /// </summary>
    public static bool TypedUAVLoadSupport_R11G11B10_FLOAT;

    /// <summary>
    /// Defines the EnableHDROutput
    /// </summary>
    public static bool EnableHDROutput;

    /// <summary>
    /// Defines the DescriptorAllocators
    /// </summary>
    public static DescriptorAllocator[] DescriptorAllocators;

    /// <summary>
    /// The AllocateDescriptor
    /// </summary>
    /// <param name="type">The <see cref="DescriptorHeapType"/></param>
    /// <param name="count">The <see cref="int"/></param>
    /// <returns>The <see cref="CpuDescriptorHandle"/></returns>
    public static CpuDescriptorHandle AllocateDescriptor(DescriptorHeapType type, int count = 1)
    {
      return DescriptorAllocators[(int)type].Allocate(count);
    }

    /// <summary>
    /// Defines the _GenerateMipsRS
    /// </summary>
    public static Core.RootSignature _GenerateMipsRS;

    /// <summary>
    /// Defines the _GenerateMipsLinearPSO
    /// </summary>
    public static ComputePSO[] _GenerateMipsLinearPSO = new ComputePipelineStateDescription[4];

    /// <summary>
    /// Defines the _GenerateMipsGammaPSO
    /// </summary>
    public static ComputePSO[] _GenerateMipsGammaPSO = new ComputePipelineStateDescription[4];

    /// <summary>
    /// Defines the _FrameIndex
    /// </summary>
    internal static UInt64 _FrameIndex;

    /// <summary>
    /// Defines the _FrameTime
    /// </summary>
    internal static float _FrameTime;
  }
}
