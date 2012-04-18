using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public delegate void DeviceMessageHandler(object sender, byte[] message);
    public interface IDevice : IDisposable {
        event DeviceMessageHandler MessageReceived;
        IList<ITest> Tests { get; }
        IList<ISensor> Sensors { get; }
        IList<ICamera> Cameras { get; }
        ICamera PrimaryCamera { get; }
        uint Id { get; }
        string DisplayName { get; }
        byte HorizontalFinPosition { get; set; }
        byte VerticalFinPosition { get; set; }
        byte FinRange { get; set; }
        byte TopFinOffset { get; set; }
        byte RightFinOffset { get; set; }
        byte BottomFinOffset { get; set; }
        byte LeftFinOffset { get; set; }
        byte Thrust { get; set; }
        bool Turbo { get; set; }
        byte Illumination { get; set; }
        PowerConfigurations PowerConfiguration { get; set; }
        void CalibrateIMU();
        void Open();
        void Close();
    }
}
