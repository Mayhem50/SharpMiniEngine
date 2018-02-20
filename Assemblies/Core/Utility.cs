using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Core
{
  public static class Utility
  {
    public static uint ToUInt(this BitArray self)
    {
      if(self.Length > 32)
        throw new ArgumentException("Argument length shall be at most 32 bits.");

      var array = new uint[1];
      self.CopyTo(array, 0);
      return array[0];
    }
  }
}
