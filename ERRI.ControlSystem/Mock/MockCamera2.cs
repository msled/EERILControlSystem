using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using EERIL.ControlSystem.Avt;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace EERIL.ControlSystem.Mock {
    public class MockCamera2 : DependencyObject, ICamera
    {
        public event FrameReadyHandler FrameReady;
        private static uint instanceCount = 0;
        private const int FULL_BIT_DEPTH = 12; // ADC max res. = 12 for Prosilica GC1380

        #region State Properties
        public static readonly DependencyProperty FocusPositionProperty = DependencyProperty.Register("FocusPosition", typeof(byte), typeof(MockCamera2), new PropertyMetadata(default(byte)));
        public static readonly DependencyProperty FrameRateProperty = DependencyProperty.Register("FrameRate", typeof(float), typeof(MockCamera2), new PropertyMetadata(default(float)));

        [Category("State")]
        [DisplayName("Name")]
        [PropertyOrder(0)]
        public string DisplayName { get; private set; }

        [Category("State")]
        [DisplayName("Id")]
        [PropertyOrder(1)]
        public uint UniqueId
        {
            get;
            private set;
        }

        [Category("State")]
        [DisplayName("Serial")]
        [PropertyOrder(2)]
        public string SerialString
        {
            get;
            private set;
        }

        [Category("State")]
        [DisplayName("Part Number")]
        [PropertyOrder(3)]
        public uint PartNumber
        {
            get;
            private set;
        }

        [Category("State")]
        [DisplayName("Version")]
        [PropertyOrder(4)]
        public uint PartVersion
        {
            get;
            private set;
        }

        [Category("State")]
        [DisplayName("Permitted Access")]
        [PropertyOrder(5)]
        public uint PermittedAccess
        {
            get;
            private set;
        }

        [Category("State")]
        [DisplayName("Interface Id")]
        [PropertyOrder(6)]
        public uint InterfaceId
        {
            get;
            private set;
        }

        [Category("State")]
        [PropertyOrder(8)]
        public uint Reference
        {
            get;
            private set;
        }

        [Category("State")]
        [DisplayName("Focus Position")]
        [PropertyOrder(9)]
        public byte FocusPosition
        {
            get
            {
                return (byte)GetValue(FocusPositionProperty);
            }
            set
            {
                SetValue(FocusPositionProperty, value);
            }
        }

        [Category("State")]
        [DisplayName("Frame Rate")]
        [PropertyOrder(10)]
        public float FrameRate
        {
            get
            {
                return (float)GetValue(FrameRateProperty);
            }
            set
            {
                SetValue(FrameRateProperty, value);
            }
        }

        [Category("State")]
        [DisplayName("Bytes Per Frame")]
        [PropertyOrder(11)]
        public uint TotalBytesPerFrame
        {get; private set;
        }
        #endregion

        #region Image Format Properties
        public static readonly DependencyProperty ImageFormatProperty = DependencyProperty.Register("ImageFormat", typeof(ImageFormat), typeof(MockCamera2), new PropertyMetadata(default(ImageFormat)));
        public static readonly DependencyProperty ImageHeightProperty = DependencyProperty.Register("ImageHeight", typeof(uint), typeof(MockCamera2), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty ImageWidthProperty = DependencyProperty.Register("ImageWidth", typeof(uint), typeof(MockCamera2), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty ImageRegionXProperty = DependencyProperty.Register("ImageRegionX", typeof(uint), typeof(MockCamera2), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty ImageRegionYProperty = DependencyProperty.Register("ImageRegionY", typeof(uint), typeof(MockCamera2), new PropertyMetadata(default(uint)));
        
        [Category("Image Format")]
        [DisplayName("Format")]
        [PropertyOrder(1)]
        public ImageFormat ImageFormat {
            get
            {
                return (ImageFormat)GetValue(ImageFormatProperty);
            }
            set
            {
                SetValue(ImageFormatProperty, value);
            }
        }
        
        [Category("Image Format")]
        [DisplayName("Height")]
        [PropertyOrder(2)]
        public uint ImageHeight {
            get
            {
                return (uint) GetValue(ImageHeightProperty);
            }
            set
            {
                if (value < 1) {
                    throw new InvalidDataException("Image height must be greater than 0.");
                }
                SetValue(ImageHeightProperty, value);
            }
        }

        [Category("Image Format")]
        [DisplayName("Width")]
        [PropertyOrder(3)]
        public uint ImageWidth {
            get {
                return (uint) GetValue(ImageWidthProperty);
            }
            set {
                if (value < 1) {
                    throw new InvalidDataException("Image width must be greater than 0.");
                }
                SetValue(ImageWidthProperty, value);
            }
        }

        [Category("Image Format")]
        [DisplayName("Region X")]
        [PropertyOrder(4)]
        public uint ImageRegionX
        {
            get
            {
                return (uint)GetValue(ImageRegionXProperty);
            }
            set
            {
                SetValue(ImageRegionXProperty, value);
            }
        }

        [Category("Image Format")]
        [DisplayName("Region Y")]
        [PropertyOrder(5)]
        public uint ImageRegionY
        {
            get
            {
                return (uint)GetValue(ImageRegionYProperty);
            }
            set
            {
                SetValue(ImageRegionYProperty, value);
            }
        }

        #endregion
        #region Digital Signal Processing Properties

        public static readonly DependencyProperty DspSubregionBottomProperty = DependencyProperty.Register("DspSubregionBottom", typeof(uint), typeof(MockCamera2), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty DspSubregionLeftProperty = DependencyProperty.Register("DspSubregionLeft", typeof(uint), typeof(MockCamera2), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty DspSubregionTopProperty = DependencyProperty.Register("DspSubregionTop", typeof(uint), typeof(MockCamera2), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty DspSubregionRightProperty = DependencyProperty.Register("DspSubregionRight", typeof(uint), typeof(MockCamera2), new PropertyMetadata(default(uint)));


        [Category("Digital Signal Processing")]
        [DisplayName("Top Subregion")]
        [PropertyOrder(1)]
        public uint DspSubregionTop
        {
            get
            {
                return (uint)GetValue(DspSubregionTopProperty);
            }
            set
            {
                if (value < 0 || value > 4294967295)
                {
                    throw new InvalidDataException("Valid DSP subregion values are between 0 and 4294967295.");
                }
                SetValue(DspSubregionTopProperty, value);
            }
        }

        [Category("Digital Signal Processing")]
        [DisplayName("Right Subregion")]
        [PropertyOrder(2)]
        public uint DspSubregionRight
        {
            get
            {
                return (uint)GetValue(DspSubregionRightProperty);
            }
            set
            {
                if (value < 0 || value > 4294967295)
                {
                    throw new InvalidDataException("Valid DSP subregion values are between 0 and 4294967295.");
                }
                SetValue(DspSubregionRightProperty, value);
            }
        }

        [Category("Digital Signal Processing")]
        [DisplayName("Bottom Subregion")]
        [PropertyOrder(3)]
        public uint DspSubregionBottom
        {
            get
            {
                return (uint)GetValue(DspSubregionBottomProperty);
            }
            set
            {
                if (value < 0 || value > 4294967295)
                {
                    throw new InvalidDataException("Valid DSP subregion values are between 0 and 4294967295.");
                }
                SetValue(DspSubregionBottomProperty, value);
            }
        }

        [Category("Digital Signal Processing")]
        [DisplayName("Left Subregion")]
        [PropertyOrder(4)]
        public uint DspSubregionLeft
        {
            get
            {
                return (uint)GetValue(DspSubregionLeftProperty);
            }
            set
            {
                SetValue(DspSubregionLeftProperty, value);
            }
        }
        #endregion

        #region Gain Properties
        public static readonly DependencyProperty GainModeProperty = DependencyProperty.Register("GainMode", typeof(GainMode), typeof(MockCamera2), new PropertyMetadata(default(GainMode)));
        public static readonly DependencyProperty GainToleranceProperty = DependencyProperty.Register("GainTolerance", typeof(uint), typeof(MockCamera2), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty MinimumGainProperty = DependencyProperty.Register("MinimumGain", typeof(uint), typeof(MockCamera2), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty MaximumGainProperty = DependencyProperty.Register("MaximumGain", typeof(uint), typeof(MockCamera2), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty GainOutliersProperty = DependencyProperty.Register("GainOutliers", typeof(uint), typeof(MockCamera2), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty GainRateProperty = DependencyProperty.Register("GainRate", typeof(uint), typeof(MockCamera2), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty GainTargetProperty = DependencyProperty.Register("GainTarget", typeof(uint), typeof(MockCamera2), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty GainProperty = DependencyProperty.Register("Gain", typeof(uint), typeof(MockCamera2), new PropertyMetadata(default(uint)));

        [Category("Gain")]
        [DisplayName("Value")]
        [PropertyOrder(1)]
        public uint Gain
        {
            get
            {
                return (uint)GetValue(GainProperty);
            }
            set
            {
                SetValue(GainProperty, value);
            }
        }

        [Category("Gain")]
        [DisplayName("Mode")]
        [PropertyOrder(2)]
        public GainMode GainMode
        {
            get
            {
                return (GainMode)GetValue(GainModeProperty);
            }
            set
            {
                SetValue(GainModeProperty, value);
            }
        }

        [Category("Gain")]
        [DisplayName("Tolerance")]
        [PropertyOrder(3)]
        public uint GainTolerance
        {
            get
            {
                return (uint)GetValue(GainToleranceProperty);
            }
            set
            {
                if (value < 0 || value > 50)
                {
                    throw new InvalidDataException("Valid values are between 0 and 50.");
                }
                SetValue(GainToleranceProperty, value);
            }
        }

        [Category("Gain")]
        [DisplayName("Minimum")]
        [PropertyOrder(4)]
        public uint MinimumGain
        {
            get
            {
                return (uint)GetValue(MinimumGainProperty);
            }
            set
            {
                SetValue(MinimumGainProperty, value);
            }
        }

        [Category("Gain")]
        [DisplayName("Maximum")]
        [PropertyOrder(5)]
        public uint MaximumGain
        {
            get
            {
                return (uint)GetValue(MaximumGainProperty);
            }
            set
            {
                SetValue(MaximumGainProperty, value);
            }
        }

        [Category("Gain")]
        [DisplayName("Outliers")]
        [PropertyOrder(6)]
        public uint GainOutliers
        {
            get
            {
                return (uint)GetValue(GainOutliersProperty);
            }
            set
            {
                if (value < 0 || value > 1000)
                {
                    throw new InvalidDataException("Valid values are between 0 and 1000.");
                }
                SetValue(GainOutliersProperty, value);
            }
        }

        [Category("Gain")]
        [DisplayName("Rate")]
        [PropertyOrder(7)]
        public uint GainRate
        {
            get
            {
                return (uint)GetValue(GainRateProperty);
            }
            set
            {
                if (value < 1 || value > 100)
                {
                    throw new InvalidDataException("Valid values are between 0 and 100.");
                }
                SetValue(GainRateProperty, value);
            }
        }

        [Category("Gain")]
        [DisplayName("Target")]
        [PropertyOrder(8)]
        public uint GainTarget
        {
            get
            {
                return (uint)GetValue(GainTargetProperty);
            }
            set
            {
                if (value < 0 || value > 100)
                {
                    throw new InvalidDataException("Valid values are between 0 and 100.");
                }
                SetValue(GainTargetProperty, value);
            }
        }
        #endregion

        #region WhiteBalance Properties
        public static readonly DependencyProperty WhiteBalanceModeProperty = DependencyProperty.Register("WhiteBalanceMode", typeof(WhiteBalanceMode), typeof(MockCamera2), new PropertyMetadata(default(WhiteBalanceMode)));
        public static readonly DependencyProperty WhiteBalanceToleranceProperty = DependencyProperty.Register("WhiteBalanceTolerance", typeof(uint), typeof(MockCamera2), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty WhiteBalanceRateProperty = DependencyProperty.Register("WhiteBalanceRate", typeof(uint), typeof(MockCamera2), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty WhiteBalanceRedProperty = DependencyProperty.Register("WhiteBalanceRed", typeof(uint), typeof(MockCamera2), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty WhiteBalanceBlueProperty = DependencyProperty.Register("WhiteBalanceBlue", typeof(uint), typeof(MockCamera2), new PropertyMetadata(default(uint)));

        [Category("White Balance")]
        [DisplayName("Mode")]
        [PropertyOrder(1)]
        public WhiteBalanceMode WhiteBalanceMode
        {
            get
            {
                return (WhiteBalanceMode)GetValue(WhiteBalanceModeProperty);
            }
            set
            {
                SetValue(WhiteBalanceModeProperty, value);
            }
        }
        [Category("White Balance")]
        [DisplayName("Tolerance")]
        [PropertyOrder(2)]
        public uint WhiteBalanceTolerance
        {
            get
            {
                return (uint)GetValue(WhiteBalanceToleranceProperty);
            }
            set
            {
                if (value < 0 || value > 50)
                {
                    throw new InvalidDataException("Valid values are between 0 and 50.");
                }
                SetValue(WhiteBalanceToleranceProperty, value);
            }
        }

        [Category("White Balance")]
        [DisplayName("Rate")]
        [PropertyOrder(3)]
        public uint WhiteBalanceRate
        {
            get
            {
                return (uint)GetValue(WhiteBalanceRateProperty);
            }
            set
            {
                if (value < 1 || value > 100)
                {
                    throw new InvalidDataException("Valid values are between 0 and 100.");
                }
                SetValue(WhiteBalanceRateProperty, value);
            }
        }

        [Category("White Balance")]
        [DisplayName("Red")]
        [PropertyOrder(4)]
        public uint WhiteBalanceRed
        {
            get
            {
                return (uint)GetValue(WhiteBalanceRedProperty);
            }
            set
            {
                SetValue(WhiteBalanceRedProperty, value);
            }
        }

        [Category("White Balance")]
        [DisplayName("Blue")]
        [PropertyOrder(5)]
        public uint WhiteBalanceBlue
        {
            get
            {
                return (uint)GetValue(WhiteBalanceBlueProperty);
            }
            set
            {
                SetValue(WhiteBalanceBlueProperty, value);
            }
        }
        #endregion

        #region Exposure Properties
        public static readonly DependencyProperty ExposureAlgorithmProperty = DependencyProperty.Register("ExposureAlgorithm", typeof(ExposureAlgorithm), typeof(MockCamera2), new PropertyMetadata(default(ExposureAlgorithm)));
        public static readonly DependencyProperty ExposureModeProperty = DependencyProperty.Register("ExposureMode", typeof(ExposureMode), typeof(MockCamera2), new PropertyMetadata(default(ExposureMode)));
        public static readonly DependencyProperty ExposureToleranceProperty = DependencyProperty.Register("ExposureTolerance", typeof(uint), typeof(MockCamera2), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty MinimumExposureProperty = DependencyProperty.Register("MinimumExposure", typeof(uint), typeof(MockCamera2), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty MaximumExposureProperty = DependencyProperty.Register("MaximumExposure", typeof(uint), typeof(MockCamera2), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty ExposureOutliersProperty = DependencyProperty.Register("ExposureOutliers", typeof(uint), typeof(MockCamera2), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty ExposureRateProperty = DependencyProperty.Register("ExposureRate", typeof(uint), typeof(MockCamera2), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty ExposureTargetProperty = DependencyProperty.Register("ExposureTarget", typeof(uint), typeof(MockCamera2), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty ExposureProperty = DependencyProperty.Register("Exposure", typeof(uint), typeof(MockCamera2), new PropertyMetadata(default(uint)));

        [Category("Exposure")]
        [DisplayName("Value")]
        [PropertyOrder(1)]
        public uint Exposure
        {
            get
            {
                return (uint)GetValue(ExposureProperty);
            }
            set
            {
                if (value < 0 || value > 60000000)
                {
                    throw new InvalidDataException("Valid values are between 0 and 60000000.");
                }
                SetValue(ExposureProperty, value);
            }
        }

        [Category("Exposure")]
        [DisplayName("Algorithm")]
        [PropertyOrder(2)]
        public ExposureAlgorithm ExposureAlgorithm
        {
            get
            {
                return (ExposureAlgorithm)GetValue(ExposureAlgorithmProperty);
            }
            set
            {
                SetValue(ExposureAlgorithmProperty, value);
            }
        }

        [Category("Exposure")]
        [DisplayName("Mode")]
        [PropertyOrder(3)]
        public ExposureMode ExposureMode
        {
            get
            {
                return (ExposureMode)GetValue(ExposureModeProperty);
            }
            set
            {
                SetValue(ExposureModeProperty, value);
            }
        }

        [Category("Exposure")]
        [DisplayName("Tolerance")]
        [PropertyOrder(4)]
        public uint ExposureTolerance
        {
            get
            {
                return (uint)GetValue(ExposureToleranceProperty);
            }
            set
            {
                if (value < 0 || value > 50)
                {
                    throw new InvalidDataException("Valid values are between 0 and 50.");
                }
                SetValue(ExposureToleranceProperty, value);
            }
        }

        [Category("Exposure")]
        [DisplayName("Minimum")]
        [PropertyOrder(5)]
        public uint MinimumExposure
        {
            get
            {
                return (uint)GetValue(MinimumExposureProperty);
            }
            set
            {
                if (value < 0 || value > 60000000)
                {
                    throw new InvalidDataException("Valid values are between 0 and 60000000.");
                }
                SetValue(MinimumExposureProperty, value);
            }
        }

        [Category("Exposure")]
        [DisplayName("Maximum")]
        [PropertyOrder(6)]
        public uint MaximumExposure
        {
            get
            {
                return (uint)GetValue(MaximumExposureProperty);
            }
            set
            {
                if (value < 0 || value > 60000000)
                {
                    throw new InvalidDataException("Valid values are between 0 and 60000000.");
                }
                SetValue(MaximumExposureProperty, value);
            }
        }

        [Category("Exposure")]
        [DisplayName("Outliers")]
        [PropertyOrder(7)]
        public uint ExposureOutliers
        {
            get
            {
                return (uint)GetValue(ExposureOutliersProperty);
            }
            set
            {
                if (value < 0 || value > 1000)
                {
                    throw new InvalidDataException("Valid values are between 0 and 1000.");
                }
                SetValue(ExposureOutliersProperty, value);
            }
        }

        [Category("Exposure")]
        [DisplayName("Rate")]
        [PropertyOrder(8)]
        public uint ExposureRate
        {
            get
            {
                return (uint)GetValue(ExposureRateProperty);
            }
            set
            {
                if (value < 1 || value > 100)
                {
                    throw new InvalidDataException("Valid values are between 0 and 100.");
                }
                SetValue(ExposureRateProperty, value);
            }
        }

        [Category("Exposure")]
        [DisplayName("Target")]
        [PropertyOrder(9)]
        public uint ExposureTarget
        {
            get
            {
                return (uint)GetValue(ExposureTargetProperty);
            }
            set
            {
                if (value < 0 || value > 100)
                {
                    throw new InvalidDataException("Valid values are between 0 and 100.");
                }
                SetValue(ExposureTargetProperty, value);
            }
        }

        #endregion

        public MockCamera2() {
            DisplayName = "Mock Camera 2 " + instanceCount;
            UniqueId = instanceCount;
            SerialString = DisplayName;
            PartNumber = 37;
            PartVersion = 2;
            PermittedAccess = instanceCount;
            InterfaceId = instanceCount;
            Reference = instanceCount++;
            FocusPosition = 90;
            FrameRate = 20;
            TotalBytesPerFrame = 1392640;
            ImageFormat = ImageFormat.Bayer8;
            ImageHeight = 1024;
            ImageWidth = 1360;
            ImageRegionX = 0;
            ImageRegionY = 0;
            DspSubregionBottom = 4294967295;
            DspSubregionLeft = 0;
            DspSubregionRight = 4294967295;
            DspSubregionTop = 0;
            GainMode = GainMode.Manual;
            GainTolerance = 5;
            MaximumGain = 27;
            MinimumGain = 0;
            GainOutliers = 0;
            GainRate = 100;
            GainTarget = 50;
            Gain = 0;
            WhiteBalanceMode = WhiteBalanceMode.Manual;
            WhiteBalanceTolerance = 5;
            WhiteBalanceRate = 100;
            WhiteBalanceRed = 133;
            WhiteBalanceBlue = 261;
            ExposureAlgorithm = ExposureAlgorithm.Mean;
            ExposureMode = ExposureMode.Manual;
            ExposureTolerance = 5;
            MaximumExposure = 500000;
            MinimumExposure = 8;
            ExposureOutliers = 0;
            ExposureRate = 100;
            ExposureTarget = 50;
            Exposure = 15000;
        }

        public void BeginCapture()
        {
 
        }

        public void EndCapture()
        {
        }

        public bool WriteBytesToSerial(byte[] buffer)
        {
            return true;
        }

        public bool ReadBytesFromSerial(byte[] buffer, ref uint recieved) {
            recieved = 0;
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

        public uint GetDepth()
        {
            switch (ImageFormat)
            {
                case ImageFormat.Mono8:
                case ImageFormat.Bayer8:
                case ImageFormat.Rgb24:
                case ImageFormat.Bgr24:
                case ImageFormat.Yuv411:
                case ImageFormat.Yuv422:
                case ImageFormat.Yuv444:
                case ImageFormat.Rgba32:
                case ImageFormat.Bgra32:
                    return 8;

                case ImageFormat.Mono12Packed:
                case ImageFormat.Bayer12Packed:
                    return 12;

                case ImageFormat.Mono16:
                case ImageFormat.Bayer16:
                case ImageFormat.Rgb48:
                    return FULL_BIT_DEPTH; // depends on hardware

                default:
                    return 0;
            }
        }

        public float GetBytesPerPixel()
        {
            switch (ImageFormat)
            {
                case ImageFormat.Mono8:
                case ImageFormat.Bayer8:
                    return 1;

                case ImageFormat.Yuv411:
                case ImageFormat.Mono12Packed:
                case ImageFormat.Bayer12Packed:
                    return 1.5F;

                case ImageFormat.Mono16:
                case ImageFormat.Bayer16:
                case ImageFormat.Yuv422:
                    return 2;

                case ImageFormat.Rgb24:
                case ImageFormat.Bgr24:
                case ImageFormat.Yuv444:
                    return 3;

                case ImageFormat.Rgba32:
                case ImageFormat.Bgra32:
                    return 4;

                case ImageFormat.Rgb48:
                    return 6;

                default:
                    return 0;
            }
        }
    };
}