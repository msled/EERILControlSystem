using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EERIL.ControlSystem.Avt
{
    public enum WhiteBalanceMode : uint
    {
        Manual = 0,
        Auto = 1,
        AutoOnce = 2
    };

    public class WhiteBalance
    {
        public WhiteBalanceMode mode;
        public uint tolerance;
        public uint rate;
        public uint red;
        public uint blue;

        public WhiteBalance()
        {
            mode = WhiteBalanceMode.Manual;
            tolerance = 5;
            rate = 100;
            red = 133;
            blue = 261;
        }
    }
}
