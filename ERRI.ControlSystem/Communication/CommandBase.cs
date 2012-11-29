namespace EERIL.ControlSystem.Communication {
    public abstract class CommandBase : ICommand {
        public byte[] Command { get; private set; }

        protected CommandBase(byte[] command) {
            this.Command = command;
        }
    }

    class ValuelessCommand : CommandBase {
        public ValuelessCommand(byte command) : base(new byte[] { command, 0x0D }) {}
    }

    public class SingleByteCommand : CommandBase {
        public SingleByteCommand(byte command, byte value) : base(new byte[] {command, value, 0x0D }) { }
    }

    public class SingleByteCommandWithModifier : CommandBase {
        public SingleByteCommandWithModifier(byte command, byte modifier, byte value2) :
            base(new byte[] { command, modifier, value2, 0x0D }) { }
    }
}
