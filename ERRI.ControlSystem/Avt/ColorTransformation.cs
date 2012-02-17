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
        public ColorTransformationMode mode;
        public float[][] values;
        public ColorTransformation() {
            mode = ColorTransformationMode.Off;
            values = new float[3][] { new float[3] {1, 0, 0},
                                      new float[3] {0, 1, 0},
                                      new float[3] {0, 0, 1}};
        }
    }
}
