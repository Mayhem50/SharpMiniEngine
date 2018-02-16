using SharpDX;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Particles
{
  public struct ParticleEffectProperties
  {
    public Color MinStartColor;
    public Color MaxStartColor;
    public Color MinEndColor;
    public Color MaxEndColor;
    public EmissionProperties EmitProperties;
    public float EmitRate;
    public Vector2 LifeMinMax;
    public Vector2 MassMinMax;
    public Vector4 Size;
    public Vector3 Spread;
    public float TotalActiveLifetime;
    public Vector4 Velocity;

    public String TexturePath;

    public static ParticleEffectProperties Create()
    {
      ParticleEffectProperties result = new ParticleEffectProperties();
      result.MinStartColor = new Color(0.8f, 0.8f, 1.0f);
      result.MaxStartColor = new Color(0.9f, 0.9f, 1.0f);
      result.MinEndColor = new Color(1.0f, 1.0f, 1.0f);
      result.MaxEndColor = new Color(1.0f, 1.0f, 1.0f);
      result.EmitProperties = EmissionProperties.Create(); //Properties passed to the shader
      result.EmitRate = 200;
      result.LifeMinMax = new Vector2(1.0f, 2.0f);
      result.MassMinMax = new Vector2(0.5f, 1.0f);
      result.Size = new Vector4(0.07f, 0.7f, 0.8f, 0.8f); // (Start size min, Start size max, End size min, End size max) 		
      result.Spread = new Vector3(0.5f, 1.5f, 0.1f);
      result.TexturePath = "Resources/Textures/sparkTex.dds";
      result.TotalActiveLifetime = 20.0f;
      result.Velocity = new Vector4(0.5f, 3.0f, -0.5f, 3.0f); // (X velocity min, X velocity max, Y velocity min, Y velocity max)

      return result;
    }
  }
}
