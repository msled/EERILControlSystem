using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using EERIL.ControlSystem.Test;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace EERIL.ControlSystem.Mock
{
    class MockDevice : IDevice
    {
        public void Dispose()
        {
        }

        private static uint instanceCount;
        public event DeviceMessageHandler MessageReceived;
        [Browsable(false)]
        public IList<ITest> Tests { get; private set; }
        [Browsable(false)]
        public IList<ISensor> Sensors { get; private set; }
        [Browsable(false)]
        public IList<ICamera> Cameras { get; private set; }
        [Browsable(false)]
        public ICamera PrimaryCamera { get; private set; }
        [Category("State")]
        [ReadOnly(true)]
        [PropertyOrder(1)]
        public uint Id { get; private set; }
        [Category("State")]
        [ReadOnly(true)]
        [PropertyOrder(2)]
        public string DisplayName { get; private set; }
        [Category("State")]
        [PropertyOrder(3)]
        public byte Illumination { get; set; }
        [Category("State")]
        [PropertyOrder(4)]
        public PowerConfigurations PowerConfiguration { get; set; }
        [Category("State")]
        [ReadOnly(true)]
        [PropertyOrder(5)]
        public bool Turbo { get; set; }
        [Category("State")]
        [ReadOnly(true)]
        [PropertyOrder(6)]
        public byte Thrust { get; set; }
        [Category("State")]
        [ReadOnly(true)]
        [PropertyOrder(7)]
        public byte HorizontalFinPosition { get; set; }
        [Category("State")]
        [ReadOnly(true)]
        [PropertyOrder(8)]
        public byte VerticalFinPosition { get; set; }
        [Category("Calibration")]
        [DisplayName("Fin Range")]
        [PropertyOrder(9)]
        public byte FinRange { get; set; }
        [Category("Calibration")]
        [DisplayName("Top Fin Offset")]
        public byte TopFinOffset { get; set; }
        [Category("Calibration")]
        [DisplayName("Right Fin Offset")]
        public byte RightFinOffset { get; set; }
        [Category("Calibration")]
        [DisplayName("Bottom Fin Offset")]
        public byte BottomFinOffset { get; set; }
        [Category("Calibration")]
        [DisplayName("Left Fin Offset")]
        public byte LeftFinOffset { get; set; }

        public void CalibrateIMU() {
            
        }

        public MockDevice()
        {
            Tests = new List<ITest>().AsReadOnly();
            Sensors = new List<ISensor>().AsReadOnly();
            Cameras = new List<ICamera>()
                          {
                              new MockCamera2(),
                              new MockCamera()
                          }.AsReadOnly();
            Id = 45 + instanceCount++;
            DisplayName = "Mock Device " + Id;
            PrimaryCamera = Cameras[0];
        }

        public void Open()
        {
        }

        public void Close()
        {
        }
    }
}
