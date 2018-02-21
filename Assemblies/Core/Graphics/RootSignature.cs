namespace Core.Graphics
{
  using SharpDX.Direct3D12;
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Linq;
  using System.Threading;

  /// <summary>
  /// Defines the <see cref="CRootParameter" />
  /// </summary>
  public class CRootParameter : IDisposable
  {
    /// <summary>
    /// The InitAsConstants
    /// </summary>
    /// <param name="register">The <see cref="int"/></param>
    /// <param name="numDwords">The <see cref="int"/></param>
    /// <param name="visibility">The <see cref="ShaderVisibility"/></param>
    public void InitAsConstants(int register, int numDwords, ShaderVisibility visibility = ShaderVisibility.All)
    {
      var constant = new RootConstants(register, 0, numDwords);
      Parameter = new RootParameter1(visibility, constant);
    }

    /// <summary>
    /// The InitAsConstantBuffer
    /// </summary>
    /// <param name="register">The <see cref="int"/></param>
    /// <param name="visibility">The <see cref="ShaderVisibility"/></param>
    public void InitAsConstantBuffer(int register, ShaderVisibility visibility = ShaderVisibility.All)
    {
      var cbuffer = new RootDescriptor1 { RegisterSpace = 0, ShaderRegister = register, Flags = RootDescriptorFlags.None };
      Parameter = new RootParameter1(visibility, cbuffer, RootParameterType.ConstantBufferView);
    }

    /// <summary>
    /// The InitAsBufferSRV
    /// </summary>
    /// <param name="register">The <see cref="int"/></param>
    /// <param name="visibility">The <see cref="ShaderVisibility"/></param>
    public void InitAsBufferSRV(int register, ShaderVisibility visibility = ShaderVisibility.All)
    {
      var buffer = new RootDescriptor1 { RegisterSpace = 0, ShaderRegister = register, Flags = RootDescriptorFlags.None };
      Parameter = new RootParameter1(visibility, buffer, RootParameterType.ShaderResourceView);
    }

    /// <summary>
    /// The InitAsBufferUAV
    /// </summary>
    /// <param name="register">The <see cref="int"/></param>
    /// <param name="visibility">The <see cref="ShaderVisibility"/></param>
    public void InitAsBufferUAV(int register, ShaderVisibility visibility = ShaderVisibility.All)
    {
      var buffer = new RootDescriptor1 { RegisterSpace = 0, ShaderRegister = register, Flags = RootDescriptorFlags.None };
      Parameter = new RootParameter1(visibility, buffer, RootParameterType.UnorderedAccessView);
    }

    /// <summary>
    /// The InitAsDescriptorRange
    /// </summary>
    /// <param name="type">The <see cref="DescriptorRangeType"/></param>
    /// <param name="register">The <see cref="int"/></param>
    /// <param name="count">The <see cref="int"/></param>
    /// <param name="visibility">The <see cref="ShaderVisibility"/></param>
    public void InitAsDescriptorRange(DescriptorRangeType type, int register, int count, ShaderVisibility visibility = ShaderVisibility.All)
    {
      InitAsDescriptorTable(1, visibility);
      SetTableRange(0, type, register, count, 0);
    }

    /// <summary>
    /// The InitAsDescriptorTable
    /// </summary>
    /// <param name="rangeCount">The <see cref="int"/></param>
    /// <param name="visibility">The <see cref="ShaderVisibility"/></param>
    public void InitAsDescriptorTable(int rangeCount, ShaderVisibility visibility)
    {
      var ranges = new List<DescriptorRange>(rangeCount);
      Parameter = new RootParameter1(visibility, ranges.ToArray());
    }

    /// <summary>
    /// The SetTableRange
    /// </summary>
    /// <param name="rangeIndex">The <see cref="int"/></param>
    /// <param name="type">The <see cref="DescriptorRangeType"/></param>
    /// <param name="register">The <see cref="int"/></param>
    /// <param name="count">The <see cref="int"/></param>
    /// <param name="space">The <see cref="int"/></param>
    public void SetTableRange(int rangeIndex, DescriptorRangeType type, int register, int count, int space = 0)
    {
      var range = Parameter.DescriptorTable[rangeIndex];
      range.RangeType = type;
      range.DescriptorCount = count;
      range.BaseShaderRegister = register;
      range.RegisterSpace = space;
      range.OffsetInDescriptorsFromTableStart = unchecked((int)UInt32.MaxValue);
    }

    /// <summary>
    /// The Dispose
    /// </summary>
    public void Dispose()
    {
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets or sets the Parameter
    /// </summary>
    public RootParameter1 Parameter { get; protected set; }
  }

  /// <summary>
  /// Defines the <see cref="CRootSignature" />
  /// </summary>
  public class CRootSignature
  {
    /// <summary>
    /// Defines the Finalized
    /// </summary>
    internal bool Finalized;

    /// <summary>
    /// Defines the ParametersCount
    /// </summary>
    internal int ParametersCount;

    /// <summary>
    /// Defines the SamplersCount
    /// </summary>
    internal int SamplersCount;

    /// <summary>
    /// Defines the DescriptorTableBitMap
    /// </summary>
    internal BitArray DescriptorTableBitMap = new BitArray(32, false);

    /// <summary>
    /// Defines the SamplerTableBitMap
    /// </summary>
    internal BitArray SamplerTableBitMap = new BitArray(32, false);

    /// <summary>
    /// Defines the DescriptorTableSize
    /// </summary>
    internal uint[] DescriptorTableSize = new uint[16];

    /// <summary>
    /// Defines the InitializedStaticSamplersCount
    /// </summary>
    protected int InitializedStaticSamplersCount;

    /// <summary>
    /// Defines the ParamArray
    /// </summary>
    protected CRootParameter[] ParamArray;

    /// <summary>
    /// Defines the SamplerArray
    /// </summary>
    protected StaticSamplerDescription[] SamplerArray;

    /// <summary>
    /// Defines the _RootSignature
    /// </summary>
    protected RootSignature _RootSignature;

    /// <summary>
    /// Defines the _HashMapMutex
    /// </summary>
    protected static object _HashMapMutex = new object();

    /// <summary>
    /// Defines the _RootSignatureHashMap
    /// </summary>
    protected static Dictionary<UInt64, RootSignature> _RootSignatureHashMap = new Dictionary<ulong, RootSignature>();

    /// <summary>
    /// Initializes a new instance of the <see cref="CRootSignature"/> class.
    /// </summary>
    /// <param name="numRootParams">The <see cref="int"/></param>
    /// <param name="numStaticSamplers">The <see cref="int"/></param>
    public CRootSignature(int numRootParams = 0, int numStaticSamplers = 0)
    {
      Finalized = false;
      ParametersCount = numRootParams;
      Reset(numRootParams, numStaticSamplers);
    }

    /// <summary>
    /// The Reset
    /// </summary>
    /// <param name="numRootParams">The <see cref="int"/></param>
    /// <param name="numStaticSamplers">The <see cref="int"/></param>
    public void Reset(int numRootParams, int numStaticSamplers)
    {
      if (numRootParams > 0)
      {
        ParamArray.ToList().ForEach(p => p.Dispose());
        ParamArray = new CRootParameter[numRootParams];
      }
      else { ParamArray = null; }

      ParametersCount = numRootParams;

      if (numStaticSamplers > 0)
      {
        SamplerArray = new StaticSamplerDescription[numStaticSamplers];
      }
      else { SamplerArray = null; }

      SamplersCount = numStaticSamplers;
      InitializedStaticSamplersCount = 0;
    }


    public CRootParameter this[int index] {
      get {
        Debug.Assert(index < ParametersCount);
        return ParamArray[index];
      }
    }
    /// <summary>
    /// The InitStaticSampler
    /// </summary>
    /// <param name="register">The <see cref="int"/></param>
    /// <param name="nonstaticSamplerDesc">The <see cref="SamplerStateDescription"/></param>
    /// <param name="visibility">The <see cref="ShaderVisibility"/></param>
    public void InitStaticSampler(int register, SamplerStateDescription nonstaticSamplerDesc, ShaderVisibility visibility = ShaderVisibility.All)
    {
      Debug.Assert(InitializedStaticSamplersCount < SamplersCount);
      ref var staticSamplerDesc = ref SamplerArray[InitializedStaticSamplersCount++];

      staticSamplerDesc.Filter = nonstaticSamplerDesc.Filter;
      staticSamplerDesc.AddressU = nonstaticSamplerDesc.AddressU;
      staticSamplerDesc.AddressV = nonstaticSamplerDesc.AddressV;
      staticSamplerDesc.AddressW = nonstaticSamplerDesc.AddressW;
      staticSamplerDesc.MipLODBias = nonstaticSamplerDesc.MipLodBias;
      staticSamplerDesc.MaxAnisotropy = nonstaticSamplerDesc.MaximumAnisotropy;
      staticSamplerDesc.ComparisonFunc = nonstaticSamplerDesc.ComparisonFunction;
      staticSamplerDesc.BorderColor = StaticBorderColor.OpaqueWhite;
      staticSamplerDesc.MinLOD = nonstaticSamplerDesc.MinimumLod;
      staticSamplerDesc.MaxLOD = nonstaticSamplerDesc.MaximumLod;
      staticSamplerDesc.ShaderRegister = register;
      staticSamplerDesc.RegisterSpace = 0;
      staticSamplerDesc.ShaderVisibility = visibility;

      if (staticSamplerDesc.AddressU == TextureAddressMode.Border ||
         staticSamplerDesc.AddressV == TextureAddressMode.Border ||
         staticSamplerDesc.AddressW == TextureAddressMode.Border)
      {
        if (nonstaticSamplerDesc.BorderColor.A == 1.0f)
        {
          if (nonstaticSamplerDesc.BorderColor.R == 1.0f) { staticSamplerDesc.BorderColor = StaticBorderColor.OpaqueWhite; }
          else { staticSamplerDesc.BorderColor = StaticBorderColor.OpaqueBlack; }
        }
        else { staticSamplerDesc.BorderColor = StaticBorderColor.TransparentBlack; }
      }
    }

    /// <summary>
    /// The Finalize
    /// </summary>
    /// <param name="name">The <see cref="string"/></param>
    /// <param name="flags">The <see cref="RootSignatureFlags"/></param>
    public void Finalize(string name, RootSignatureFlags flags)
    {
      if (Finalized) { return; }

      Debug.Assert(InitializedStaticSamplersCount == SamplersCount);

      var rootDesc = new RootSignatureDescription1
      {
        Parameters = ParamArray.Select(p => p.Parameter).ToArray(),
        StaticSamplers = SamplerArray,
        Flags = flags
      };

      var hashCode = Hash.HashState<int>(new[] { (int)rootDesc.Flags });
      hashCode = Hash.HashState(rootDesc.StaticSamplers, SamplersCount, hashCode);

      for (int param = 0; param < ParametersCount; param++)
      {
        var rootParam = rootDesc.Parameters[param];

        if (rootParam.ParameterType == RootParameterType.DescriptorTable)
        {
          Debug.Assert(rootParam.DescriptorTable.Length != 0);

          hashCode = Hash.HashState(rootParam.DescriptorTable, rootParam.DescriptorTable.Length, hashCode);

          if (rootParam.DescriptorTable[0].RangeType == DescriptorRangeType.Sampler) { SamplerTableBitMap[param] = true; }
          else { DescriptorTableBitMap[param] = true; }

          for (int tableRange = 0; tableRange < rootParam.DescriptorTable.Length; tableRange++)
          {
            DescriptorTableSize[param] += (uint)rootParam.DescriptorTable[tableRange].DescriptorCount;
          }
        }
        else { hashCode = Hash.HashState(new[] { rootParam }, 1, hashCode); }
      }

      bool firstCompile = false;
      {
        lock (_HashMapMutex)
        {
          if (!_RootSignatureHashMap.TryGetValue(hashCode, out var rootSig))
          {
            _RootSignatureHashMap.Add(hashCode, null);
            firstCompile = true;
          }

          if (firstCompile)
          {
            _RootSignature = rootSig = Globals.Device.CreateRootSignature(rootDesc.Serialize());
            _RootSignature.Name = name;
            _RootSignatureHashMap[hashCode] = _RootSignature;
          }

          else
          {
            while (rootSig == null) { Thread.Yield(); }
            _RootSignature = rootSig;
          }

          Finalized = true;
        }
      }
    }
  }
}
