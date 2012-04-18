using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EERIL.DeviceControls
{
    public struct AccelerationSample
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public long Timestamp { get; set; }
    }
}
