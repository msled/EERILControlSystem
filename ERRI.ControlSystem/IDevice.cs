using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using EERIL.ControlSystem.Avt;
using EERIL.ControlSystem.Test;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;

namespace EERIL.ControlSystem {
	public delegate void FrameReadyHandler(object sender, IFrame frame);
	public delegate void DeviceMessageHandler(string message);
	public interface IDevice : IDisposable {
		event FrameReadyHandler FrameReady;
		event DeviceMessageHandler MessageReceived;
		IList<ITest> Tests { get; }
		IList<ISensor> Sensors { get; }
	    uint Id { get; }
		string DisplayName { get; }
		uint ImageHeight { get; }
		uint ImageWidth { get; }
		uint ImageDepth { get; }
		uint ColorCode { get; }
		PixelFormat PixelFormat { get; }
		byte HorizontalFinPosition { get; set; }
        byte VerticalFinPosition { get; set; }
        byte TopFinOffset { get; set; }
        byte RightFinOffset { get; set; }
        byte BottomFinOffset { get; set; }
        byte LeftFinOffset { get; set; }
		byte Thrust { get; set; }
		PowerConfigurations PowerConfiguration { get; set; }
		Bitmap CaptureImage(uint timeout);
		void StartVideoCapture(uint timeout);
		void StopVideoCapture();
		void PrepareForGrab(ref uint dcamMode, ref uint colorCode, ref uint width, ref uint height);
		void GetImage(Bitmap bitmap, uint timeout);
	    void Open();
	    void Close();
	}
}
