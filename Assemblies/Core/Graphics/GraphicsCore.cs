using SharpDX.Direct3D;
using SharpDX.Direct3D12;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Graphics
{
  public static class GraphicsCore
  {
#if DEBUG
    static Guid WKPDID_D3DDebugObjectName = new Guid(0x429b8c22, 0x9188, 0x4b0c, 0x87, 0x42, 0xac, 0xb0, 0xbf, 0x85, 0xc2, 0x00);
#endif

    public static void Initialize()
    {

    }
    public static void Resize(uint width, uint height)
    {

    }
    public static void Terminate()
    {

    }
    public static void Shutdown()
    {

    }
    public static void Present()
    {

    }

    public static UInt64 FrameCount => _FrameIndex;
    public static float FrameTime => _FrameTime;
    public static float FrameRate => _FrameTime == 0.0f ? 0.0f : 1.0f / _FrameTime;


    public static uint DisplayWidth;
    public static uint DisplayHeight;
    public static Device Device;
    public static CommandListManager CommandListManager;
    public static ContextManager ContextManager;
    public static FeatureLevel FeatureLevel;
    public static bool TypedUAVLoadSupport_R11G11B10_FLOAT;
    public static bool EnableHDROutput;

    public static DescriptorAllocator[] DescriptorAllocators;
    public static CpuDescriptorHandle AllocateDescriptor(DescriptorHeapType type, int count = 1)
    {
      return DescriptorAllocators[(int)type].Allocate(count);
    }

    public static Core.RootSignature _GenerateMipsRS;
    public static ComputePSO[] _GenerateMipsLinearPSO = new ComputePipelineStateDescription[4];
    public static ComputePSO[] _GenerateMipsGammaPSO = new ComputePipelineStateDescription[4];

    internal static UInt64 _FrameIndex; 
    internal static float _FrameTime;
  }
}
