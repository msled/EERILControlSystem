using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EERIL.ControlSystem.Avt
{
    public enum ExposureAlgorithm : uint
    {
        Mean = 0,
        FitRange = 1
    };

    public enum ExposureMode : uint
    {
        Manual = 0,
        Auto = 1,
        AutoOnce = 2,
        External = 3
    };

    public class Exposure
    {
        public ExposureAlgorithm algorithm;
        public ExposureMode mode;
        public uint tolerance;
        public uint max;
        public uint min;
        public uint outliers;
        public uint rate;
        public uint target;
        public uint value;

        public Exposure(
            )
        {
            algorithm = ExposureAlgorithm.Mean;
            mode = ExposureMode.Manual;
            tolerance = 5;
            max = 500000;
            min = 8;
            outliers = 0;
            rate = 100;
            target = 50;
            value = 15000;
        }
    }
}
