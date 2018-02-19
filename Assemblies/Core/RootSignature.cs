using System;
using System.Linq;
using System.Collections.Generic;
using SharpDX.Direct3D12;

namespace Core
{
  public static class RootParameterHelper
  {
    public static RootParameter InitAsConstants(int register, int numDwords, ShaderVisibility visibility = ShaderVisibility.All)
    {
      RootConstants constant = new RootConstants(register, 0, numDwords);
      return new RootParameter(visibility, constant);
    }
    public static RootParameter InitAsConstantBuffer(int register, ShaderVisibility visibility = ShaderVisibility.All)
    {
      RootDescriptor cbuffer = new RootDescriptor(register, 0);
      return new RootParameter(visibility, cbuffer, RootParameterType.ConstantBufferView);
    }
    public static RootParameter InitAsBufferSRV(int register, ShaderVisibility visibility = ShaderVisibility.All)
    {
      RootDescriptor buffer = new RootDescriptor(register, 0);
      return new RootParameter(visibility, buffer, RootParameterType.ShaderResourceView);
    }
    public static RootParameter InitAsBufferUAV(int register, ShaderVisibility visibility = ShaderVisibility.All)
    {
      RootDescriptor buffer = new RootDescriptor(register, 0);
      return new RootParameter(visibility, buffer, RootParameterType.UnorderedAccessView);
    }
    public static RootParameter InitAsDescriptorRange(DescriptorRangeType type, int register, int count, ShaderVisibility visibility = ShaderVisibility.All)
    {
      RootParameter result = InitAsDescriptorTable(1, visibility);
      SetTableRange(ref result, 0, type, register, count, 0);
      return result;
    }
    public static RootParameter InitAsDescriptorTable(int rangeCount, ShaderVisibility visibility)
    {
      List<DescriptorRange> ranges = new List<DescriptorRange>(rangeCount);
      return new RootParameter(visibility, ranges.ToArray());
    }
    public static void SetTableRange(ref RootParameter param, int rangeIndex, DescriptorRangeType type, int register, int count, int space = 0)
    {
      DescriptorRange range = param.DescriptorTable[rangeIndex];
      range.RangeType = type;
      range.DescriptorCount = count;
      range.BaseShaderRegister = register;
      range.RegisterSpace = space;
      range.OffsetInDescriptorsFromTableStart = unchecked((int)UInt32.MaxValue);
    }
  }
}