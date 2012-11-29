using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EERIL.ControlSystem.Avt;
using EERIL.ControlSystem.Communication;

namespace EERIL.ControlSystem.v4.Commands {
    public class SingleByteMsledCommandWithModifier : SingleByteCommandWithModifier {
        public SingleByteMsledCommandWithModifier(CommandCode command, Modifier modifier, byte value) :
            base((byte)command, (byte)modifier, value) { }
    }
}
