using System;
using System.Collections.Generic;
using System.Text;

namespace Core
{
  public static class BitScanner
  {
    public static bool BitScanForward(uint value, out int index)
    {
      if(value == 0)
      {
        index = 0;
        return false;
      }

      int[] MultiplyDeBruijnBitPosition = { 0, 1, 28, 2, 29, 14, 24, 3, 30, 22, 20, 15, 25, 17, 4, 8,
                                                                  31, 27, 13, 23, 21, 19, 16, 7, 26, 12, 18, 6, 11, 5, 10, 9 };

      index = MultiplyDeBruijnBitPosition[(value & -value) * 0x077CB531];
      return true;
    }
    public static bool BitScanReverse(uint value, out int index)
    {
      if (value == 0)
      {
        index = 0;
        return false;
      }

      int[] MultiplyDeBruijnBitPosition = { 0, 9, 1, 10, 13, 21, 2, 29, 11, 14, 16, 18, 22, 25, 3, 30,
                                            8, 12, 20, 28, 15, 17, 24, 7, 19, 27, 23, 6, 26, 5, 4, 31 };
      value |= value >> 1;
      value |= value >> 2;
      value |= value >> 4;
      value |= value >> 8;
      value |= value >> 16;

      index = MultiplyDeBruijnBitPosition[value * 0x07C4ACDD >> 27];
      return true;
    }
    public static bool BitScanForward64(ulong value, out int index)
    {
      if (value == 0)
      {
        index = 0;
        return false;
      }

      int[] MultiplyDeBruijnBitPosition = { 0, 1, 17, 2, 18, 50, 3, 57,
                                            47, 19, 22, 51, 29, 4, 33, 58,
                                            15, 48, 20, 27, 25, 23, 52, 41,
                                            54, 30, 38, 5, 43, 34, 59, 8,
                                            63, 16, 49, 56, 46, 21, 28, 32,
                                            14, 26, 24, 40, 53, 37, 42, 7,
                                            62, 55, 45, 31, 13, 39, 36, 6,
                                            61, 44, 12, 35, 60, 11, 10, 9 };

      index = MultiplyDeBruijnBitPosition[((ulong)((long)value & -(long)value)) * 0x37E84A99DAE458F];
      return true;
    }
    public static bool BitScanReverse64(ulong value, out int index)
    {
      if (value == 0)
      {
        index = 0;
        return false;
      }

      int[] MultiplyDeBruijnBitPosition = { 0, 1, 17, 2, 18, 50, 3, 57,
                                            47, 19, 22, 51, 29, 4, 33, 58,
                                            15, 48, 20, 27, 25, 23, 52, 41,
                                            54, 30, 38, 5, 43, 34, 59, 8,
                                            63, 16, 49, 56, 46, 21, 28, 32,
                                            14, 26, 24, 40, 53, 37, 42, 7,
                                            62, 55, 45, 31, 13, 39, 36, 6,
                                            61, 44, 12, 35, 60, 11, 10, 9 };
      value |= value >> 1;
      value |= value >> 2;
      value |= value >> 4;
      value |= value >> 8;
      value |= value >> 16;
      value |= value >> 32;
      value = value & ~(value >> 1);

      index = MultiplyDeBruijnBitPosition[value * 0x37E84A99DAE458F >> 58];
      return true;
    }
  }
}
