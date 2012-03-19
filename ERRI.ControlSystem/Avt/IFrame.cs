using System;
using System.Drawing;
using System.Windows.Media.Imaging;
using PvNET;

namespace EERIL.ControlSystem.Avt {
    public interface IFrame : IDisposable {
        uint AncillarySize { get; }
        tBayerPattern BayerPattern { get; }
        uint BitDepth { get; }
        byte[] Buffer { get; }
        tImageFormat Format { get; }
        uint FrameCount { get; }
        uint Height { get; }
        uint RegionX { get; }
        uint RegionY { get; }
        uint Size { get; }
        long Timestamp { get; }
        uint Width { get; }
        Bitmap ToBitmap();
        BitmapSource ToBitmapSource();
    }
}
