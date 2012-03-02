using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EERIL.ControlSystem.Avt
{
    public enum GainMode : uint
    {
        Manual = 0,
        Auto = 1,
        AutoOnce = 2
    };

    public class Gain
    {
        public GainMode mode;
        public uint tolerance;
        public uint max;
        public uint min;
        public uint outliers;
        public uint rate;
        public uint target;
        public uint value;

        public Gain()
        {
            mode = GainMode.Manual;
            tolerance = 5;
            max = 27;
            min = 0;
            outliers = 0;
            rate = 100;
            target = 50;
            value = 0;
        }
    }
}
