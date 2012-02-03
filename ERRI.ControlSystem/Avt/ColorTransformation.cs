using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PvNET;

namespace EERIL.ControlSystem.Avt
{
    public enum ColorTransformationMode : uint
    {
        Off = 0,
        Manual = 1,
        Temp5600K = 2
    };

    public class ColorTransformation
    {
        public ColorTransformationMode Mode;
        public float ValueBB;
        public float ValueBG;
        public float ValueBR;
        public float ValueGB;
        public float ValueGG;
        public float ValueGR;
        public float ValueRB;
        public float ValueRG;
        public float ValueRR;
    }
}
