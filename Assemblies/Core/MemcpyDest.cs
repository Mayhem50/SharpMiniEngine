using Microsoft.Analytics.Interfaces;
using Microsoft.Analytics.Types.Sql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Core
{
  public struct MemcpyDest
  {
    public long Data;
    public long RowPitch;
    public long SlicePitch;
  }
}