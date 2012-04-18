using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using EERIL.ControlSystem.Test;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace EERIL.ControlSystem
{
    internal interface IDeviceMetadata
    {
        [Browsable(false)]
        IList<ITest> Tests { get; }
        [Browsable(false)]
        IList<ISensor> Sensors { get; }
        [Browsable(false)]
        IList<ICamera> Cameras { get; }
        [Browsable(false)]
        ICamera PrimaryCamera { get; }
        [Category("State")]
        [ReadOnly(true)]
        [PropertyOrder(1)]
        uint Id { get; }
        [Category("State")]
        [ReadOnly(true)]
        [PropertyOrder(2)]
        string DisplayName { get; }
        [Category("State")]
        [PropertyOrder(3)]
        byte Illumination { get; set; }
        [Category("State")]
        [PropertyOrder(4)]
        PowerConfigurations PowerConfiguration { get; set; }
        [Category("State")]
        [ReadOnly(true)]
        [PropertyOrder(5)]
        bool Turbo { get; set; }
        [Category("State")]
        [ReadOnly(true)]
        [PropertyOrder(6)]
        byte Thrust { get; set; }
        [Category("State")]
        [ReadOnly(true)]
        [PropertyOrder(7)]
        byte HorizontalFinPosition { get; set; }
        [Category("State")]
        [ReadOnly(true)]
        [PropertyOrder(8)]
        byte VerticalFinPosition { get; set; }
        [Category("Calibration")]
        [DisplayName("Fin Range")]
        [PropertyOrder(9)]
        byte FinRange { get; set; }
        [Category("Calibration")]
        [DisplayName("Top Fin Offset")]
        byte TopFinOffset { get; set; }
        [Category("Calibration")]
        [DisplayName("Right Fin Offset")]
        byte RightFinOffset { get; set; }
        [Category("Calibration")]
        [DisplayName("Bottom Fin Offset")]
        byte BottomFinOffset { get; set; }
        [Category("Calibration")]
        [DisplayName("Left Fin Offset")]
        byte LeftFinOffset { get; set; }
    }
}
