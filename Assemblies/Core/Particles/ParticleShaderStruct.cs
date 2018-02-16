using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using SharpDX;

namespace Core.Particles
{
  [StructLayout(LayoutKind.Sequential, Pack = 16)]
  public struct EmissionProperties
  {
    public Vector3 LastEmitPosW;
    public float EmitSpeed;
    public Vector3 EmitPosW;
    public float FloorHeight;
    public Vector3 EmitDirW;
    public float Restitution;
    public Vector3 EmitRightW;
    public float EmitterVelocitySensitivity;
    public Vector3 EmitUpW;
    public UInt32 MaxParticles;
    public Vector3 Gravity;
    public UInt32 TextureID;
    public Vector3 EmissiveColor;
    public float pad1;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
    public Int4[] RandIndex;

    public static EmissionProperties Create()
    {
      EmissionProperties result = new EmissionProperties();
      result.EmitPosW = result.LastEmitPosW = Vector3.Zero;
      result.EmitDirW = Vector3.UnitZ;
      result.EmitRightW = Vector3.UnitX;
      result.EmitUpW = Vector3.UnitY;
      result.Restitution = 0.6f;
      result.FloorHeight = -0.7f;
      result.EmitSpeed = 1.0f;
      result.Gravity = new Vector3(0.0f, 5.0f, 0.0f);
      result.MaxParticles = 500;

      return result;
    }
  }

  public struct ParticleSpawnData
  {
    public float AgeRate;
    public float RotationSpeed;
    public float StartSize;
    public float EndSize;
    public Vector3 Velocity;
    public float Mass;
    public Vector3 SpreadOffset;
    public float Random;
    public Color StartColor;
    public Color EndColor;
  }

  struct ParticleMotion
  {
    public Vector3 Position;
    public float Mass;
    public Vector3 Velocity;
    public float Age;
    public float Rotation;
    public UInt32 ResetDataIndex;
  };

  struct ParticleVertex
  {
    public Vector3 Position;
    public Vector4 Color;
    public float Size;
    public UInt32 TextureID;
  };

  struct ParticleScreenData
  {
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    public float[] Corner;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    public float[] RcpSize;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] Color;
    public float Depth;
    public float TextureIndex;
    public float TextureLevel;
    public UInt32 Bounds;
  };
}
