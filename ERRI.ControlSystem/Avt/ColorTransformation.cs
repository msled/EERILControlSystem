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
        private float[][] values;
        public void ColorTransformation() {
            mode = 0;
            values = new float[3][] { new float[3] {1, 0, 0},
                                      new float[3] {0, 1, 0},
                                      new float[3] {0, 0, 1}};
        }
        public float[][] getValues() {
            return values;
        }
        public float getRR() {
            return values[0][0];
        }
        public void setRR(float value) {
            if (value >= 0.0 && value <= 2.0) {
                values[0][0] = value;
            }
        }
        public float getRG() {
            return values[0][1];
        }
        public void setRG(float value)
        {
            if (value >= 0.0 && value <= 2.0)
            {
                values[0][1] = value;
            }
        }
        public float getRB() {
            return values[0][2];
        }
        public void setRB(float value)
        {
            if (value >= 0.0 && value <= 2.0)
            {
                values[0][2] = value;
            }
        }
        public float getGR()
        {
            return values[1][0];
        }
        public void setGR(float value)
        {
            if (value >= 0.0 && value <= 2.0)
            {
                values[1][0] = value;
            }
        }
        public float getGG()
        {
            return values[1][1];
        }
        public void setGG(float value)
        {
            if (value >= 0.0 && value <= 2.0)
            {
                values[1][1] = value;
            }
        }
        public float getGB()
        {
            return values[1][2];
        }
        public void setGB(float value)
        {
            if (value >= 0.0 && value <= 2.0)
            {
                values[1][2] = value;
            }
        }
        public float getBR()
        {
            return values[2][0];
        }
        public void setBR(float value)
        {
            if (value >= 0.0 && value <= 2.0)
            {
                values[2][0] = value;
            }
        }
        public float getBG()
        {
            return values[2][1];
        }
        public void setBG(float value)
        {
            if (value >= 0.0 && value <= 2.0)
            {
                values[2][1] = value;
            }
        }
        public float getBB()
        {
            return values[2][2];
        }
        public void setBB(float value)
        {
            if (value >= 0.0 && value <= 2.0)
            {
                values[2][2] = value;
            }
        }
    }
}
