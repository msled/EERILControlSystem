using System;
using PvNET;

namespace EERIL.ControlSystem.Avt {
    public delegate void FrameReadyHandler(object sender, IFrame frame);
    public interface ICamera {
        event FrameReadyHandler FrameReady;
        string DisplayName { get; }
        uint InterfaceId { get; }
        uint Reference { get; }
        tInterface InterfaceType { get; }
        uint PartNumber { get; }
        uint PartVersion { get; }
        uint PermittedAccess { get; }
        string SerialString { get; }
        uint UniqueId { get; }
        float Temperature { get; }
        uint ImageHeight { get; set; }
        uint ImageWidth { get; set; }
        uint ImageDepth { get; }
        float BytesPerPixel { get; }
        ColorTransformation ColorTransformation { get; set; }
        DSP DSP { get; set; }
        tImageFormat ImageFormat { get; set; }
        EdgeFilter EdgeFilter { get; set; }
        Gain Gain { get; set; }
        Exposure Exposure { get; set; }
        float Gamma { get; set; }
        float Hue { get; set; }
        void BeginCapture(tImageFormat fmt);
        void EndCapture();
        bool ReadBytesFromSerial(byte[] buffer, ref uint recieved);
        bool WriteBytesToSerial(byte[] buffer);
        void Open();
        void AdjustPacketSize();
        void Close();
    }
}
