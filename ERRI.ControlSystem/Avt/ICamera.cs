using System;
namespace EERIL.ControlSystem.Avt {
	public delegate void FrameReadyHandler(object sender, IFrame frame);
	public interface ICamera {
		event FrameReadyHandler FrameReady;
		string DisplayName { get; }
		uint InterfaceId { get; }
	    uint Reference { get; }
        Interface InterfaceType { get; }
		uint PartNumber { get; }
		uint PartVersion { get; }
		uint PermittedAccess { get; }
		string SerialString { get; }
		uint UniqueId { get; }
        uint ImageHeight { get; set; }
        uint ImageWidth { get; set; }
        ImageFormat ImageFormat { get; set; }
        uint ColorCode { get; set; }
	    void BeginCapture();
	    void EndCapture();
	    bool ReadBytesFromSerial(byte[] buffer, ref uint recieved);
	    bool WriteBytesToSerial(byte[] buffer);
		void Open();
		void Close();
	}
}
