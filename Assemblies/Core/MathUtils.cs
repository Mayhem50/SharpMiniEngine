using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Core
{
  public static class MathUtils
  {
    public static uint AlignUpWithMask(this uint self, long mask) => (uint)((self + mask) & ~mask);
    public static int AlignUpWithMask(this int self, long mask) => (int)(self + mask & ~mask);
    public static float AlignUpWithMask(this float self, long mask) => ((long)self + mask) & ~mask;

    public static long AlignUpWithMask(this long self, long mask) => (self + mask) & ~mask;

    public static ulong AlignUpWithMask(this ulong self, long mask) => (ulong)(((long)self + mask) & ~mask);

    public static bool IsAligned<T>(T[] value, int alignement) where T : struct
    {
      var ptr = Marshal.UnsafeAddrOfPinnedArrayElement(value, 0);
      return 0 == (ptr.ToInt64() & (alignement - 1));
    }
  }
}
