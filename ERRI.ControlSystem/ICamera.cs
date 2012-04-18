using System;
using PvNET;

namespace EERIL.ControlSystem {
    public delegate void FrameReadyHandler(object sender, IFrame frame);
    public interface ICamera {
        event FrameReadyHandler FrameReady;
        string DisplayName { get; }
        string SerialString { get; }
        uint UniqueId { get; }
        float FrameRate { get; set; }
        void BeginCapture();
        void EndCapture();
        bool ReadBytesFromSerial(byte[] buffer, ref uint recieved);
        bool WriteBytesToSerial(byte[] buffer);
        void Open();
        void AdjustPacketSize();
        void Close();
    }
}
