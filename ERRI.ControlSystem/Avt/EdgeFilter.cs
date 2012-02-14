using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EERIL.ControlSystem.Avt
{
    public enum EdgeFilter : uint
    {
        Smooth2 = 0,
        Smooth1 = 1,
        Off = 2,
        Sharpen1 = 3,
        Sharpen2 = 4
    };
}
