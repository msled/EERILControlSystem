using System;

namespace EERIL.ControlSystem.Communication {
    public delegate void MessageHandler(IMessage message);

    public interface ICommunicationsManager : IDisposable {
        bool Connected { get; }
        event MessageHandler MessageReceived;
        void TransmitCommand(ICommand command);
        bool ReadBytesFromSerial(byte[] buffer, ref uint recieved);
        void OnMessageReceived(IMessage message);
    }
}
