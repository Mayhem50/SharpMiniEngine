using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Core
{
  public static class Hash
  {
    public static UInt64 HashState<T>(T[] stateDesc, int count = 1, UInt64 hash = 2166136261U) where T : struct
    {
      Debug.Assert((Marshal.SizeOf<T>(stateDesc[0]) & 3) == 0 && typeof(T).StructLayoutAttribute.Pack >= 4, "State object is not word-aligned");
      return HashRange(stateDesc, count, hash);
    }
    public static ulong HashRange<T>(T[] stateDesc, int count, ulong hash) where T : struct
    {
      for(int idx = 0; idx < count; idx++)
      {
        var ptr = Marshal.UnsafeAddrOfPinnedArrayElement(stateDesc, idx);
        hash = 16777619U * hash ^ (ulong)ptr.ToInt64();
      }
      return hash;
    }
  }
}
