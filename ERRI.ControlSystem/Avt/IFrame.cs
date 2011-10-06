using System;
using System.Windows.Media.Imaging;

namespace EERIL.ControlSystem.Avt {
	public interface IFrame : IDisposable {
		uint AncillarySize { get; }
		BayerPattern BayerPattern { get; }
		uint BitDepth { get; }
		byte[] Buffer { get; }
		ImageFormat Format { get; }
		uint FrameCount { get; }
		uint Height { get; }
		uint RegionX { get; }
		uint RegionY { get; }
		uint Size { get; }
		long Timestamp { get; }
		uint Width { get; }
	    BitmapSource ToBitmapSource();
	}
}
