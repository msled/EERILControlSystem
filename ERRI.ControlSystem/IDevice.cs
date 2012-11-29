﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using EERIL.ControlSystem.Avt;
using EERIL.ControlSystem.Test;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;
using PvNET;

namespace EERIL.ControlSystem {
    public delegate void DeviceFrameReadyHandler(object sender, IFrame frame);
    public delegate void DeviceMessageHandler(object sender, byte[] message);
    public interface IDevice : IDisposable {
        event DeviceFrameReadyHandler FrameReady;
        event DeviceMessageHandler MessageReceived;
        IList<ITest> Tests { get; }
        IList<ISensor> Sensors { get; }
        uint Id { get; }
        string DisplayName { get; }
        float BytesPerPixel { get; }
        uint ImageHeight { get; set; }
        uint ImageWidth { get; set; }
        uint ImageDepth { get;}
        float FrameRate { get; set; }
        tImageFormat PixelFormat { get; set; }
        byte HorizontalFinPosition { get; set; }
        byte VerticalFinPosition { get; set; }
        byte FocusPosition { get; set; }
        byte BuoyancyPosition { get; set; }
        byte FinRange { get; set; }
        byte TopFinOffset { get; set; }
        byte RightFinOffset { get; set; }
        byte BottomFinOffset { get; set; }
        byte LeftFinOffset { get; set; }
        byte Thrust { get; set; }
        bool Turbo { get; set; }
        bool CTD { get; set; }
        bool ThrustInversion { get; set; }
        byte Illumination { get; set; }
        PowerConfigurations PowerConfiguration { get; set; }
        void StartVideoCapture(uint timeout);
        void StopVideoCapture();
        //void PrepareForGrab(ref uint dcamMode, ref uint colorCode, ref uint width, ref uint height);
        //void GetImage(Bitmap bitmap, uint timeout);
        void Open();
        void Close();
    }
}
