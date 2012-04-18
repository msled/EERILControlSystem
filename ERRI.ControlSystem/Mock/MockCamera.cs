using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using PvNET;
using System.ComponentModel;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace EERIL.ControlSystem.Mock
{
    class MockCamera : ICamera
    {
        private static uint instanceCount;
        public event FrameReadyHandler FrameReady;
        [Category("State")]
        [PropertyOrder(1)]
        public string DisplayName { get; private set; }
        [Category("State")]
        [PropertyOrder(2)]
        public string SerialString { get; private set; }
        [Category("State")]
        [PropertyOrder(3)]
        public uint UniqueId { get; private set; }
        [Category("Capture")]
        [PropertyOrder(1)]
        public float FrameRate { get; set; }

        public MockCamera()
        {
            DisplayName = "Mock Camera " + instanceCount.ToString(CultureInfo.InvariantCulture);
            SerialString = DisplayName;
            UniqueId = Convert.ToUInt32(45 + instanceCount++);
            FrameRate = 20;
        }
        public void BeginCapture()
        {
        }

        public void EndCapture()
        {
        }

        public bool ReadBytesFromSerial(byte[] buffer, ref uint recieved)
        {
            recieved = 0;
            return true;
        }

        public bool WriteBytesToSerial(byte[] buffer)
        {
            return true;
        }

        public void Open()
        {
        }

        public void AdjustPacketSize()
        {
        }

        public void Close()
        {
        }
    }
}
