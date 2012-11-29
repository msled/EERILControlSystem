using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EERIL.ControlSystem.Avt;
using EERIL.ControlSystem.Communication;

namespace EERIL.ControlSystem.v4.Commands {
    class SingleByteMsledCommand : SingleByteCommand {
        public SingleByteMsledCommand(CommandCode command, byte value) : base((byte)command, value) {}
    }
}
