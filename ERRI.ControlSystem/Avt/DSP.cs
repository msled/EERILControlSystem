using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EERIL.ControlSystem.Avt
{
   class DSP
    {
        public uint bottom;
        public uint left;
        public uint right;
        public uint top;

        public void DSP() {
            bottom = 4294967295;
            left = 4294967295;
            right = 4294967295;
            top = 4294967295;
        }
    }
}
