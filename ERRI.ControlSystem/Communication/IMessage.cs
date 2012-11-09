namespace EERIL.ControlSystem.Communication {
    public interface IMessage {
        MessageType Type { get; }
        byte[] Message { get; }
    }
}
