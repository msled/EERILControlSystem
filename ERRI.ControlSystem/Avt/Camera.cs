using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using PvNET;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using MessageBox = System.Windows.MessageBox;
using Timer = System.Threading.Timer;

namespace EERIL.ControlSystem.Avt {
    public class Camera : DependencyObject, ICamera {
        private const int FRAME_POOL_SIZE = 10;
        private const UInt32 MAX_PACKET_SIZE = 16456;
        private const int FULL_BIT_DEPTH = 12; // ADC max res. = 12 for Prosilica GC1380
        private readonly Dictionary<IntPtr, byte[]> buffers = new Dictionary<IntPtr, byte[]>();
        private readonly tFrameCallback callback;
        private readonly byte[] heartbeat = new byte[] {0xA6, 0x0D};
        private readonly Timer heartbeatTimer;
        private CameraReference camera = new CameraReference();
        private tCameraInfo cameraInfo;
        private GCHandle[] frameBufferHandles;
        private GCHandle[] framePoolHandles;
        private tFrame[] frames;

        public Camera(tCameraInfo cameraInfo) {
            heartbeatTimer = new Timer(Heartbeat, null, 100, 100);
            this.cameraInfo = cameraInfo;
            callback = OnFrameReady;
        }

        #region ICamera Members

        public event FrameReadyHandler FrameReady;

        public void BeginCapture() {
            tErr err;
            if (!camera.HasValue) {
                err = tErr.eErrUnavailable;
                throw new PvException(err);
            }

            err = Pv.CaptureStart(camera.Value);
            if (err != tErr.eErrSuccess) {
                goto error;
            }

            frameBufferHandles = new GCHandle[FRAME_POOL_SIZE];
            framePoolHandles = new GCHandle[FRAME_POOL_SIZE];
            frames = new tFrame[FRAME_POOL_SIZE];

            uint bufferSize = 0;
            err = Pv.AttrUint32Get(camera.Value, "TotalBytesPerFrame", ref bufferSize);
            if (err != tErr.eErrSuccess) {
                goto error;
            }
            byte[] buffer;
            GCHandle bufferHandle, frameHandle;
            tFrame frame;
            IntPtr framePointer;
            for (int count = FRAME_POOL_SIZE - 1; count >= 0; count--) {
                buffer = new byte[bufferSize];
                bufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                frameBufferHandles[count] = bufferHandle;
                frame = new tFrame {ImageBuffer = bufferHandle.AddrOfPinnedObject(), ImageBufferSize = bufferSize, AncillaryBufferSize = 0};
                frames[count] = frame;
                frameHandle = GCHandle.Alloc(frame, GCHandleType.Pinned);
                framePoolHandles[count] = frameHandle;
                framePointer = frameHandle.AddrOfPinnedObject();
                buffers.Add(framePointer, buffer);
                err = Pv.CaptureQueueFrame(camera.Value, framePointer, callback);
                if (err != tErr.eErrSuccess) {
                    goto error;
                }
            }
            FrameRate = 15;
            err = Pv.AttrEnumSet(camera.Value, "FrameStartTriggerMode", "FixedRate");
            if (err != tErr.eErrSuccess) {
                goto error;
            }
            err = Pv.AttrEnumSet(camera.Value, "AcquisitionMode", "Continuous");
            if (err != tErr.eErrSuccess) {
                goto error;
            }
            err = Pv.CommandRun(camera.Value, "AcquisitionStart");
            if (err != tErr.eErrSuccess) {
                goto error;
            }
            return;
            error:
            EndCapture();

            throw new PvException(err);
        }

        public void EndCapture() {
            if (!camera.HasValue) {
                throw new PvException(tErr.eErrUnavailable);
            }
            tErr err = Pv.CaptureQueueClear(camera.Value);
            ValidatePvResponse(err);

            foreach (GCHandle handle in framePoolHandles) {
                handle.Free();
            }
            foreach (GCHandle handle in frameBufferHandles) {
                handle.Free();
            }
            frames = null;
            err = Pv.CommandRun(camera.Value, "AcquisitionStop");
            ValidatePvResponse(err);
            err = Pv.CaptureEnd(camera.Value);
            ValidatePvResponse(err);
        }

        public bool WriteBytesToSerial(byte[] buffer)
        {
            bool success = false;
            if (!camera.HasValue)
            {
                return false;
            }
        
            try
            {
                success = CameraSerial.WriteBytesToSerialIo(camera.Value, buffer, Convert.ToUInt32(buffer.LongLength));
            }
            catch (PvException ex)
            {
                ValidatePvResponse(ex.Error);
            }

            if(success)
                heartbeatTimer.Change(100, 100);

            return success;
        }

        public bool ReadBytesFromSerial(byte[] buffer, ref uint recieved) {
            bool success = false;
            if (!camera.HasValue)
            {
                return false;
            }
        
            try {
                success = CameraSerial.ReadBytesFromSerialIO(camera.Value, buffer, Convert.ToUInt32(buffer.LongLength), ref recieved);
            } catch (PvException ex) {
                ValidatePvResponse(ex.Error);
            }

        return success;
        }

        public void Open() {
            if (!camera.HasValue) {
                uint cameraId;
                Pv.CameraClose(cameraInfo.UniqueId);
                
                tErr err = Pv.CameraOpen(cameraInfo.UniqueId, tAccessFlags.eAccessMaster, out cameraId);
                ValidatePvResponse(err);

                camera.Value = cameraId;
                CameraSerial.Setup(cameraId);
                WriteBytesToSerial(new byte[] {0x6F, 0x0D});
                this.Dispatcher.BeginInvoke(new Action(delegate
                {
                    this.ExposureMode = ExposureMode.Auto;
                    this.WhiteBalanceMode = WhiteBalanceMode.Auto;
                    this.GainMode = GainMode.Auto;
                }
                    ));
            }
        }

        public void AdjustPacketSize() {
            if (!camera.HasValue) {
                throw new PvException(tErr.eErrUnavailable);
            }
            
            tErr err = Pv.CaptureAdjustPacketSize(camera.Value, MAX_PACKET_SIZE);
            ValidatePvResponse(err);
        }

        public void Close() {
            if (camera.HasValue) {
                
                tErr err = Pv.CameraClose(camera.Value);
                ValidatePvResponse(err);
                camera = null;
            }
        }

        #endregion

        #region State Properties

        public static readonly DependencyProperty FocusPositionProperty = DependencyProperty.Register("FocusPosition", typeof (byte), typeof (Camera), new PropertyMetadata(default(byte)));
        public static readonly DependencyProperty FrameRateProperty = DependencyProperty.Register("FrameRate", typeof (float), typeof (Camera), new PropertyMetadata(default(float)));

        [Category("State")]
        [DisplayName("Part Number")]
        [PropertyOrder(3)]
        public uint PartNumber {
            get {
                return cameraInfo.PartNumber;
            }
        }

        [Category("State")]
        [DisplayName("Version")]
        [PropertyOrder(4)]
        public uint PartVersion {
            get {
                return cameraInfo.PartVersion;
            }
        }

        [Category("State")]
        [DisplayName("Permitted Access")]
        [PropertyOrder(5)]
        public uint PermittedAccess {
            get {
                return cameraInfo.PermittedAccess;
            }
        }

        [Category("State")]
        [DisplayName("Interface Id")]
        [PropertyOrder(6)]
        public uint InterfaceId {
            get {
                return cameraInfo.InterfaceId;
            }
        }

        [Category("State")]
        [DisplayName("Interface Type")]
        [PropertyOrder(7)]
        public tInterface InterfaceType {
            get {
                return cameraInfo.InterfaceType;
            }
        }

        [Category("State")]
        [PropertyOrder(8)]
        public uint Reference {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                return camera.Value;
            }
        }

        [Category("State")]
        [DisplayName("Focus Position")]
        [PropertyOrder(9)]
        public byte FocusPosition {
            get {
                return (byte) GetValue(FocusPositionProperty);
            }
            set {
                if (!WriteBytesToSerial(new byte[] {0x66, value, 0x0D})) {
                    throw new Exception("Failed to transmit focusposition to device.");
                }
                SetValue(FocusPositionProperty, value);
            }
        }

        [Category("State")]
        [DisplayName("Bytes Per Frame")]
        [PropertyOrder(11)]
        public uint TotalBytesPerFrame {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                uint value = 0;
                
                tErr err = Pv.AttrUint32Get(camera.Value, "TotalBytesPerFrame", ref value);
                ValidatePvResponse(err);
                return value;
            }
        }

        [Category("State")]
        [DisplayName("Name")]
        [PropertyOrder(0)]
        public string DisplayName {
            get {
                return cameraInfo.DisplayName;
            }
        }

        [Category("State")]
        [DisplayName("Id")]
        [PropertyOrder(1)]
        public uint UniqueId {
            get {
                return cameraInfo.UniqueId;
            }
        }

        [Category("State")]
        [DisplayName("Serial")]
        [PropertyOrder(2)]
        public string SerialString {
            get {
                return cameraInfo.SerialString;
            }
        }

        [Category("State")]
        [DisplayName("Frame Rate")]
        [PropertyOrder(10)]
        public float FrameRate {
            get {
                float value = 0;
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                
                tErr err = Pv.AttrFloat32Get(camera.Value, "FrameRate", ref value);
                ValidatePvResponse(err);
                this.Dispatcher.BeginInvoke(new Action(() => SetValue(FrameRateProperty, value)));
                return (float) GetValue(FrameRateProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                
                tErr err = Pv.AttrFloat32Set(camera.Value, "FrameRate", value);
                ValidatePvResponse(err);
                this.Dispatcher.BeginInvoke(new Action(() => SetValue(FrameRateProperty, value)));
            }
        }

        #endregion

        #region Image Format Properties

        public static readonly DependencyProperty ImageFormatProperty = DependencyProperty.Register("ImageFormat", typeof (ImageFormat), typeof (Camera), new PropertyMetadata(default(ImageFormat)));
        public static readonly DependencyProperty ImageHeightProperty = DependencyProperty.Register("ImageHeight", typeof (uint), typeof (Camera), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty ImageWidthProperty = DependencyProperty.Register("ImageWidth", typeof (uint), typeof (Camera), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty ImageRegionXProperty = DependencyProperty.Register("ImageRegionX", typeof (uint), typeof (Camera), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty ImageRegionYProperty = DependencyProperty.Register("ImageRegionY", typeof (uint), typeof (Camera), new PropertyMetadata(default(uint)));

        [Category("Image Format")]
        [DisplayName("Format")]
        [PropertyOrder(1)]
        public ImageFormat ImageFormat {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                var buffer = new StringBuilder(16);
                uint read = 0;
                
                tErr err = Pv.AttrEnumGet(camera.Value, "PixelFormat", buffer, 16, ref read);
                ValidatePvResponse(err);
                var value = (ImageFormat) Enum.Parse(typeof (ImageFormat), buffer.ToString(), true);
                SetValue(ImageFormatProperty, value);
                return (ImageFormat) GetValue(ImageFormatProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                
                tErr err = Pv.AttrEnumSet(camera.Value, "PixelFormat", value.ToString());
                ValidatePvResponse(err);
                SetValue(ImageFormatProperty, value);
            }
        }

        [Category("Image Format")]
        [DisplayName("Height")]
        [PropertyOrder(2)]
        public uint ImageHeight {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                uint value = 0;
                
                tErr err = Pv.AttrUint32Get(camera.Value, "Height", ref value);
                ValidatePvResponse(err);
                SetValue(ImageHeightProperty, value);
                return (uint) GetValue(ImageHeightProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                if (value < 1) {
                    throw new InvalidDataException("Image height must be greater than 0.");
                }
                
                tErr err = Pv.AttrUint32Set(camera.Value, "Height", value);
                ValidatePvResponse(err);
                SetValue(ImageHeightProperty, value);
            }
        }

        [Category("Image Format")]
        [DisplayName("Width")]
        [PropertyOrder(3)]
        public uint ImageWidth {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                uint value = 0;
                
                tErr err = Pv.AttrUint32Get(camera.Value, "Width", ref value);
                ValidatePvResponse(err);
                SetValue(ImageWidthProperty, value);
                return (uint) GetValue(ImageWidthProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                if (value < 1) {
                    throw new InvalidDataException("Image width must be greater than 0.");
                }
                
                tErr err = Pv.AttrUint32Set(camera.Value, "Width", value);
                ValidatePvResponse(err);

                SetValue(ImageWidthProperty, value);
            }
        }

        [Category("Image Format")]
        [DisplayName("Region X")]
        [PropertyOrder(4)]
        public uint ImageRegionX {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                uint value = 0;
                
                tErr err = Pv.AttrUint32Get(camera.Value, "RegionX", ref value);
                ValidatePvResponse(err);
                SetValue(ImageRegionXProperty, value);
                return (uint) GetValue(ImageRegionXProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                
                tErr err = Pv.AttrUint32Set(camera.Value, "RegionX", value);
                ValidatePvResponse(err);

                SetValue(ImageRegionXProperty, value);
            }
        }

        [Category("Image Format")]
        [DisplayName("Region Y")]
        [PropertyOrder(5)]
        public uint ImageRegionY {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                uint value = 0;
                
                tErr err = Pv.AttrUint32Get(camera.Value, "RegionY", ref value);
                ValidatePvResponse(err);
                SetValue(ImageRegionYProperty, value);
                return (uint) GetValue(ImageRegionYProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                
                tErr err = Pv.AttrUint32Set(camera.Value, "RegionY", value);
                ValidatePvResponse(err);

                SetValue(ImageRegionYProperty, value);
            }
        }

        #endregion

        #region Digital Signal Processing Properties

        public static readonly DependencyProperty DspSubregionBottomProperty = DependencyProperty.Register("DspSubregionBottom", typeof (uint), typeof (Camera), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty DspSubregionLeftProperty = DependencyProperty.Register("DspSubregionLeft", typeof (uint), typeof (Camera), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty DspSubregionTopProperty = DependencyProperty.Register("DspSubregionTop", typeof (uint), typeof (Camera), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty DspSubregionRightProperty = DependencyProperty.Register("DspSubregionRight", typeof (uint), typeof (Camera), new PropertyMetadata(default(uint)));


        [Category("Digital Signal Processing")]
        [DisplayName("Top Subregion")]
        [PropertyOrder(1)]
        public uint DspSubregionTop {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                uint value = 0;
                
                tErr err = Pv.AttrUint32Get(camera.Value, "DSPSubregionTop", ref value);
                ValidatePvResponse(err);
                SetValue(DspSubregionTopProperty, value);
                return (uint) GetValue(DspSubregionTopProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                if (value < 0 || value > 4294967295) {
                    throw new InvalidDataException("Valid DSP subregion values are between 0 and 4294967295.");
                }
                
                tErr err = Pv.AttrUint32Set(camera.Value, "DSPSubregionTop", value);
                ValidatePvResponse(err);
                SetValue(DspSubregionTopProperty, value);
            }
        }

        [Category("Digital Signal Processing")]
        [DisplayName("Right Subregion")]
        [PropertyOrder(2)]
        public uint DspSubregionRight {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                uint value = 0;
                
                tErr err = Pv.AttrUint32Get(camera.Value, "DSPSubregionRight", ref value);
                ValidatePvResponse(err);
                SetValue(DspSubregionRightProperty, value);
                return (uint) GetValue(DspSubregionRightProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                if (value < 0 || value > 4294967295) {
                    throw new InvalidDataException("Valid DSP subregion values are between 0 and 4294967295.");
                }
                
                tErr err = Pv.AttrUint32Set(camera.Value, "DSPSubregionRight", value);
                ValidatePvResponse(err);
                SetValue(DspSubregionRightProperty, value);
            }
        }

        [Category("Digital Signal Processing")]
        [DisplayName("Bottom Subregion")]
        [PropertyOrder(3)]
        public uint DspSubregionBottom {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                uint value = 0;
                
                tErr err = Pv.AttrUint32Get(camera.Value, "DSPSubregionBottom", ref value);
                ValidatePvResponse(err);
                SetValue(DspSubregionBottomProperty, value);
                return (uint) GetValue(DspSubregionBottomProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                if (value < 0 || value > 4294967295) {
                    throw new InvalidDataException("Valid DSP subregion values are between 0 and 4294967295.");
                }
                
                tErr err = Pv.AttrUint32Set(camera.Value, "DSPSubregionBottom", value);
                ValidatePvResponse(err);
                SetValue(DspSubregionBottomProperty, value);
            }
        }

        [Category("Digital Signal Processing")]
        [DisplayName("Left Subregion")]
        [PropertyOrder(4)]
        public uint DspSubregionLeft {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                uint value = 0;
                
                tErr err = Pv.AttrUint32Get(camera.Value, "DSPSubregionLeft", ref value);
                ValidatePvResponse(err);
                SetValue(DspSubregionLeftProperty, value);
                return (uint) GetValue(DspSubregionLeftProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                if (value < 0 || value > 4294967295) {
                    throw new InvalidDataException("Valid DSP subregion values are between 0 and 4294967295.");
                }
                
                tErr err = Pv.AttrUint32Set(camera.Value, "DSPSubregionLeft", value);
                ValidatePvResponse(err);
                SetValue(DspSubregionLeftProperty, value);
            }
        }

        #endregion

        #region Gain Properties

        public static readonly DependencyProperty GainModeProperty = DependencyProperty.Register("GainMode", typeof (GainMode), typeof (Camera), new PropertyMetadata(default(GainMode)));
        public static readonly DependencyProperty GainToleranceProperty = DependencyProperty.Register("GainTolerance", typeof (uint), typeof (Camera), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty MinimumGainProperty = DependencyProperty.Register("MinimumGain", typeof (uint), typeof (Camera), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty MaximumGainProperty = DependencyProperty.Register("MaximumGain", typeof (uint), typeof (Camera), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty GainOutliersProperty = DependencyProperty.Register("GainOutliers", typeof (uint), typeof (Camera), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty GainRateProperty = DependencyProperty.Register("GainRate", typeof (uint), typeof (Camera), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty GainTargetProperty = DependencyProperty.Register("GainTarget", typeof (uint), typeof (Camera), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty GainProperty = DependencyProperty.Register("Gain", typeof (uint), typeof (Camera), new PropertyMetadata(default(uint)));

        [Category("Gain")]
        [DisplayName("Value")]
        [PropertyOrder(1)]
        public uint Gain {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                uint value = 0;
                
                tErr err = Pv.AttrUint32Get(camera.Value, "GainValue", ref value);
                ValidatePvResponse(err);
                this.Dispatcher.BeginInvoke(new Action(() => SetValue(GainProperty, value)));
                return (uint) GetValue(GainProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                
                tErr err = Pv.AttrUint32Set(camera.Value, "GainValue", value);
                ValidatePvResponse(err);
                this.Dispatcher.BeginInvoke(new Action(() => SetValue(GainProperty, value)));
            }
        }

        [Category("Gain")]
        [DisplayName("Mode")]
        [PropertyOrder(2)]
        public GainMode GainMode {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                var buffer = new StringBuilder(16);
                uint read = 0;
                
                tErr err = Pv.AttrEnumGet(camera.Value, "GainMode", buffer, 16, ref read);
                ValidatePvResponse(err);
                var value = (GainMode) Enum.Parse(typeof (GainMode), buffer.ToString(), true);
                SetValue(GainModeProperty, value);
                return (GainMode) GetValue(GainModeProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                
                tErr err = Pv.AttrEnumSet(camera.Value, "GainMode", value.ToString());
                ValidatePvResponse(err);
                SetValue(GainModeProperty, value);
            }
        }

        [Category("Gain")]
        [DisplayName("Tolerance")]
        [PropertyOrder(3)]
        public uint GainTolerance {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                uint value = 0;
                
                tErr err = Pv.AttrUint32Get(camera.Value, "GainAutoAdjustTol", ref value);
                ValidatePvResponse(err);
                SetValue(GainToleranceProperty, value);
                return (uint) GetValue(GainToleranceProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                if (value < 0 || value > 50) {
                    throw new InvalidDataException("Valid values are between 0 and 50.");
                }
                
                tErr err = Pv.AttrUint32Set(camera.Value, "GainAutoAdjustTol", value);
                ValidatePvResponse(err);
                SetValue(GainToleranceProperty, value);
            }
        }

        [Category("Gain")]
        [DisplayName("Minimum")]
        [PropertyOrder(4)]
        public uint MinimumGain {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                uint value = 0;
                
                tErr err = Pv.AttrUint32Get(camera.Value, "GainAutoMin", ref value);
                ValidatePvResponse(err);
                SetValue(MinimumGainProperty, value);
                return (uint) GetValue(MinimumGainProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                
                tErr err = Pv.AttrUint32Set(camera.Value, "GainAutoMin", value);
                ValidatePvResponse(err);
                SetValue(MinimumGainProperty, value);
            }
        }

        [Category("Gain")]
        [DisplayName("Maximum")]
        [PropertyOrder(5)]
        public uint MaximumGain {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                uint value = 0;
                
                tErr err = Pv.AttrUint32Get(camera.Value, "GainAutoMax", ref value);
                ValidatePvResponse(err);
                SetValue(MaximumGainProperty, value);
                return (uint) GetValue(MaximumGainProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                
                tErr err = Pv.AttrUint32Set(camera.Value, "GainAutoMax", value);
                ValidatePvResponse(err);
                SetValue(MaximumGainProperty, value);
            }
        }

        [Category("Gain")]
        [DisplayName("Outliers")]
        [PropertyOrder(6)]
        public uint GainOutliers {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                uint value = 0;
                
                tErr err = Pv.AttrUint32Get(camera.Value, "GainAutoOutliers", ref value);
                ValidatePvResponse(err);
                SetValue(GainOutliersProperty, value);
                return (uint) GetValue(GainOutliersProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                if (value < 0 || value > 1000) {
                    throw new InvalidDataException("Valid values are between 0 and 1000.");
                }
                
                tErr err = Pv.AttrUint32Set(camera.Value, "GainAutoOutliers", value);
                ValidatePvResponse(err);
                SetValue(GainOutliersProperty, value);
            }
        }

        [Category("Gain")]
        [DisplayName("Rate")]
        [PropertyOrder(7)]
        public uint GainRate {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                uint value = 0;
                
                tErr err = Pv.AttrUint32Get(camera.Value, "GainAutoRate", ref value);
                ValidatePvResponse(err);
                SetValue(GainRateProperty, value);
                return (uint) GetValue(GainRateProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                if (value < 1 || value > 100) {
                    throw new InvalidDataException("Valid values are between 0 and 100.");
                }
                
                tErr err = Pv.AttrUint32Set(camera.Value, "GainAutoRate", value);
                ValidatePvResponse(err);
                SetValue(GainRateProperty, value);
            }
        }

        [Category("Gain")]
        [DisplayName("Target")]
        [PropertyOrder(8)]
        public uint GainTarget {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                uint value = 0;
                
                tErr err = Pv.AttrUint32Get(camera.Value, "GainAutoTarget", ref value);
                ValidatePvResponse(err);
                SetValue(GainTargetProperty, value);
                return (uint) GetValue(GainTargetProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                if (value < 0 || value > 100) {
                    throw new InvalidDataException("Valid values are between 0 and 100.");
                }
                
                tErr err = Pv.AttrUint32Set(camera.Value, "GainAutoTarget", value);
                ValidatePvResponse(err);
                SetValue(GainTargetProperty, value);
            }
        }

        #endregion

        #region WhiteBalance Properties

        public static readonly DependencyProperty WhiteBalanceModeProperty = DependencyProperty.Register("WhiteBalanceMode", typeof (WhiteBalanceMode), typeof (Camera), new PropertyMetadata(default(WhiteBalanceMode)));
        public static readonly DependencyProperty WhiteBalanceToleranceProperty = DependencyProperty.Register("WhiteBalanceTolerance", typeof (uint), typeof (Camera), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty WhiteBalanceRateProperty = DependencyProperty.Register("WhiteBalanceRate", typeof (uint), typeof (Camera), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty WhiteBalanceRedProperty = DependencyProperty.Register("WhiteBalanceRed", typeof (uint), typeof (Camera), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty WhiteBalanceBlueProperty = DependencyProperty.Register("WhiteBalanceBlue", typeof (uint), typeof (Camera), new PropertyMetadata(default(uint)));

        [Category("White Balance")]
        [DisplayName("Mode")]
        [PropertyOrder(1)]
        public WhiteBalanceMode WhiteBalanceMode {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                var buffer = new StringBuilder(16);
                uint read = 0;
                
                tErr err = Pv.AttrEnumGet(camera.Value, "WhitebalMode", buffer, 16, ref read);
                ValidatePvResponse(err);
                var value = (WhiteBalanceMode) Enum.Parse(typeof (WhiteBalanceMode), buffer.ToString(), true);
                this.Dispatcher.BeginInvoke(new Action(()=>SetValue(WhiteBalanceModeProperty, value)));
                return (WhiteBalanceMode) GetValue(WhiteBalanceModeProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                
                tErr err = Pv.AttrEnumSet(camera.Value, "WhitebalMode", value.ToString());
                ValidatePvResponse(err);
                this.Dispatcher.BeginInvoke(new Action(() => SetValue(WhiteBalanceModeProperty, value)));
            }
        }

        [Category("White Balance")]
        [DisplayName("Tolerance")]
        [PropertyOrder(2)]
        public uint WhiteBalanceTolerance {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                uint value = 0;
                
                tErr err = Pv.AttrUint32Get(camera.Value, "WhitebalAutoAdjustTol", ref value);
                ValidatePvResponse(err);
                SetValue(WhiteBalanceToleranceProperty, value);
                return (uint) GetValue(WhiteBalanceToleranceProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                if (value < 0 || value > 50) {
                    throw new InvalidDataException("Valid values are between 0 and 50.");
                }
                
                tErr err = Pv.AttrUint32Set(camera.Value, "WhitebalAutoAdjustTol", value);
                ValidatePvResponse(err);
                SetValue(WhiteBalanceToleranceProperty, value);
            }
        }

        [Category("White Balance")]
        [DisplayName("Rate")]
        [PropertyOrder(3)]
        public uint WhiteBalanceRate {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                uint value = 0;
                
                tErr err = Pv.AttrUint32Get(camera.Value, "WhitebalAutoRate", ref value);
                ValidatePvResponse(err);
                SetValue(WhiteBalanceRateProperty, value);
                return (uint) GetValue(WhiteBalanceRateProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                if (value < 1 || value > 100) {
                    throw new InvalidDataException("Valid values are between 0 and 100.");
                }
                
                tErr err = Pv.AttrUint32Set(camera.Value, "WhitebalAutoRate", value);
                ValidatePvResponse(err);
                SetValue(WhiteBalanceRateProperty, value);
            }
        }

        [Category("White Balance")]
        [DisplayName("Red")]
        [PropertyOrder(4)]
        public uint WhiteBalanceRed {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                uint value = 0;
                
                tErr err = Pv.AttrUint32Get(camera.Value, "WhitebalValueRed", ref value);
                ValidatePvResponse(err);
                SetValue(WhiteBalanceRedProperty, value);
                return (uint) GetValue(WhiteBalanceRedProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                
                tErr err = Pv.AttrUint32Set(camera.Value, "WhitebalValueRed", value);
                ValidatePvResponse(err);
                SetValue(WhiteBalanceRedProperty, value);
            }
        }

        [Category("White Balance")]
        [DisplayName("Blue")]
        [PropertyOrder(5)]
        public uint WhiteBalanceBlue {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                uint value = 0;
                
                tErr err = Pv.AttrUint32Get(camera.Value, "WhitebalBlueValue", ref value);
                ValidatePvResponse(err);
                SetValue(WhiteBalanceBlueProperty, value);
                return (uint) GetValue(WhiteBalanceBlueProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                
                tErr err = Pv.AttrUint32Set(camera.Value, "WhitebalBlueValue", value);
                ValidatePvResponse(err);
                SetValue(WhiteBalanceBlueProperty, value);
            }
        }

        #endregion

        #region Exposure Properties

        public static readonly DependencyProperty ExposureAlgorithmProperty = DependencyProperty.Register("ExposureAlgorithm", typeof (ExposureAlgorithm), typeof (Camera), new PropertyMetadata(default(ExposureAlgorithm)));
        public static readonly DependencyProperty ExposureModeProperty = DependencyProperty.Register("ExposureMode", typeof (ExposureMode), typeof (Camera), new PropertyMetadata(default(ExposureMode)));
        public static readonly DependencyProperty ExposureToleranceProperty = DependencyProperty.Register("ExposureTolerance", typeof (uint), typeof (Camera), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty MinimumExposureProperty = DependencyProperty.Register("MinimumExposure", typeof (uint), typeof (Camera), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty MaximumExposureProperty = DependencyProperty.Register("MaximumExposure", typeof (uint), typeof (Camera), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty ExposureOutliersProperty = DependencyProperty.Register("ExposureOutliers", typeof (uint), typeof (Camera), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty ExposureRateProperty = DependencyProperty.Register("ExposureRate", typeof (uint), typeof (Camera), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty ExposureTargetProperty = DependencyProperty.Register("ExposureTarget", typeof (uint), typeof (Camera), new PropertyMetadata(default(uint)));
        public static readonly DependencyProperty ExposureProperty = DependencyProperty.Register("Exposure", typeof (uint), typeof (Camera), new PropertyMetadata(default(uint)));

        [Category("Exposure")]
        [DisplayName("Value")]
        [PropertyOrder(1)]
        public uint Exposure {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                uint value = 0;
                
                tErr err = Pv.AttrUint32Get(camera.Value, "ExposureValue", ref value);
                ValidatePvResponse(err);
                SetValue(ExposureProperty, value);
                return (uint) GetValue(ExposureProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                if (value < 0 || value > 60000000) {
                    throw new InvalidDataException("Valid values are between 0 and 60000000.");
                }
                
                tErr err = Pv.AttrUint32Set(camera.Value, "ExposureValue", value);
                ValidatePvResponse(err);
                SetValue(ExposureProperty, value);
            }
        }

        [Category("Exposure")]
        [DisplayName("Algorithm")]
        [PropertyOrder(2)]
        public ExposureAlgorithm ExposureAlgorithm {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                var buffer = new StringBuilder(16);
                uint read = 0;
                
                tErr err = Pv.AttrEnumGet(camera.Value, "ExposureAutoAlg", buffer, 16, ref read);
                ValidatePvResponse(err);
                var value = (ExposureMode) Enum.Parse(typeof (ExposureMode), buffer.ToString(), true);
                SetValue(ExposureTargetProperty, value);
                return (ExposureAlgorithm) GetValue(ExposureAlgorithmProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                
                tErr err = Pv.AttrEnumSet(camera.Value, "ExposureAutoAlg", value.ToString());
                ValidatePvResponse(err);
                SetValue(ExposureAlgorithmProperty, value);
            }
        }

        [Category("Exposure")]
        [DisplayName("Mode")]
        [PropertyOrder(3)]
        public ExposureMode ExposureMode {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                var buffer = new StringBuilder(16);
                uint read = 0;
                
                tErr err = Pv.AttrEnumGet(camera.Value, "ExposureMode", buffer, 16, ref read);
                ValidatePvResponse(err);
                var value = (ExposureMode) Enum.Parse(typeof (ExposureMode), buffer.ToString(), true);
                this.Dispatcher.BeginInvoke(new Action(() => SetValue(ExposureModeProperty, value)));
                return (ExposureMode) GetValue(ExposureModeProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                
                tErr err = Pv.AttrEnumSet(camera.Value, "ExposureMode", value.ToString());
                ValidatePvResponse(err);
                this.Dispatcher.BeginInvoke(new Action(() => SetValue(ExposureModeProperty, value)));
            }
        }

        [Category("Exposure")]
        [DisplayName("Tolerance")]
        [PropertyOrder(4)]
        public uint ExposureTolerance {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                uint value = 0;
                
                tErr err = Pv.AttrUint32Get(camera.Value, "ExposureAutoAdjustTol", ref value);
                ValidatePvResponse(err);
                SetValue(ExposureToleranceProperty, value);
                return (uint) GetValue(ExposureToleranceProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                if (value < 0 || value > 50) {
                    throw new InvalidDataException("Valid values are between 0 and 50.");
                }
                
                tErr err = Pv.AttrUint32Set(camera.Value, "ExposureAutoAdjustTol", value);
                ValidatePvResponse(err);
                SetValue(ExposureToleranceProperty, value);
            }
        }

        [Category("Exposure")]
        [DisplayName("Minimum")]
        [PropertyOrder(5)]
        public uint MinimumExposure {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                uint value = 0;
                
                tErr err = Pv.AttrUint32Get(camera.Value, "ExposureAutoMin", ref value);
                ValidatePvResponse(err);
                SetValue(MinimumExposureProperty, value);
                return (uint) GetValue(MinimumExposureProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                if (value < 0 || value > 60000000) {
                    throw new InvalidDataException("Valid values are between 0 and 60000000.");
                }
                
                tErr err = Pv.AttrUint32Set(camera.Value, "ExposureAutoMin", value);
                ValidatePvResponse(err);
                SetValue(MinimumExposureProperty, value);
            }
        }

        [Category("Exposure")]
        [DisplayName("Maximum")]
        [PropertyOrder(6)]
        public uint MaximumExposure {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                uint value = 0;
                
                tErr err = Pv.AttrUint32Get(camera.Value, "ExposureAutoMax", ref value);
                ValidatePvResponse(err);
                SetValue(MaximumExposureProperty, value);
                return (uint) GetValue(MaximumExposureProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                if (value < 0 || value > 60000000) {
                    throw new InvalidDataException("Valid values are between 0 and 60000000.");
                }
                
                tErr err = Pv.AttrUint32Set(camera.Value, "ExposureAutoMax", value);
                ValidatePvResponse(err);
                SetValue(MaximumExposureProperty, value);
            }
        }

        [Category("Exposure")]
        [DisplayName("Outliers")]
        [PropertyOrder(7)]
        public uint ExposureOutliers {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                uint value = 0;
                
                tErr err = Pv.AttrUint32Get(camera.Value, "ExposureAutoOutliers", ref value);
                ValidatePvResponse(err);
                SetValue(ExposureOutliersProperty, value);
                return (uint) GetValue(ExposureOutliersProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                if (value < 0 || value > 1000) {
                    throw new InvalidDataException("Valid values are between 0 and 1000.");
                }
                
                tErr err = Pv.AttrUint32Set(camera.Value, "ExposureAutoOutliers", value);
                ValidatePvResponse(err);
                SetValue(ExposureOutliersProperty, value);
            }
        }

        [Category("Exposure")]
        [DisplayName("Rate")]
        [PropertyOrder(8)]
        public uint ExposureRate {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                uint value = 0;
                
                tErr err = Pv.AttrUint32Get(camera.Value, "ExposureAutoRate", ref value);
                ValidatePvResponse(err);
                SetValue(ExposureRateProperty, value);
                return (uint) GetValue(ExposureRateProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                if (value < 1 || value > 100) {
                    throw new InvalidDataException("Valid values are between 0 and 100.");
                }
                
                tErr err = Pv.AttrUint32Set(camera.Value, "ExposureAutoRate", value);
                ValidatePvResponse(err);
                SetValue(ExposureRateProperty, value);
            }
        }

        [Category("Exposure")]
        [DisplayName("Target")]
        [PropertyOrder(9)]
        public uint ExposureTarget {
            get {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                uint value = 0;
                
                tErr err = Pv.AttrUint32Get(camera.Value, "ExposureAutoTarget", ref value);
                ValidatePvResponse(err);
                SetValue(ExposureTargetProperty, value);
                return (uint) GetValue(ExposureTargetProperty);
            }
            set {
                if (!camera.HasValue) {
                    throw new PvException(tErr.eErrUnavailable);
                }
                if (value < 0 || value > 100) {
                    throw new InvalidDataException("Valid values are between 0 and 100.");
                }
                
                tErr err = Pv.AttrUint32Set(camera.Value, "ExposureAutoTarget", value);
                ValidatePvResponse(err);
                SetValue(ExposureTargetProperty, value);
            }
        }

        #endregion

        protected void OnFrameReady(IntPtr framePointer)
        {
            var tFrame = (tFrame)Marshal.PtrToStructure(framePointer, typeof(tFrame));
            var frame = new Frame(this, framePointer, tFrame, buffers[framePointer]);
            if (FrameReady != null)
            {
                FrameReadyHandler eventHandler = FrameReady;
                Delegate[] delegates = eventHandler.GetInvocationList();
                foreach (FrameReadyHandler handler in delegates)
                {
                    var dispatcherObject = handler.Target as DispatcherObject;
                    if (dispatcherObject != null && !dispatcherObject.CheckAccess())
                    {
                        dispatcherObject.Dispatcher.Invoke(DispatcherPriority.DataBind, handler, this, frame);
                    }
                    else
                    {
                        handler(this, frame);
                    }
                }
            }
        }

        internal void ReleaseFrame(IntPtr framePointer) {
            if (!camera.HasValue) {
                throw new PvException(tErr.eErrUnavailable);
            }
            
            tErr err = Pv.CaptureQueueFrame(camera.Value, framePointer, callback);
            ValidatePvResponse(err);
        }

        public void Heartbeat(object state) {
            WriteBytesToSerial(heartbeat);
        }

        public uint GetDepth() {
            switch (ImageFormat) {
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

        public float GetBytesPerPixel() {
            switch (ImageFormat) {
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

        private void ValidatePvResponse(tErr err) {
            switch (err) {
                case tErr.eErrBandwidth:
                case tErr.eErrDataLost:
                case tErr.eErrQueueFull:
                case tErr.eErrTimeout:
                case tErr.eErrUnavailable:
                case tErr.eErrUnplugged:
                    camera.ConnectionIssues = true;
                    throw new ConnectionInteruptedException();
            }
            camera.ConnectionIssues = false;
        }

        #region Nested type: CameraReference

        private sealed class CameraReference {
            private uint camera;
            private bool connectionIssues;
            private Object lockObject = new object();
            private IAsyncResult asyncResult;
            private int trys = 1;
            private ConnectionLostDialog dialog;

            internal CameraReference() {
                Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
                 dialog = new ConnectionLostDialog();
            }

            public bool ConnectionIssues {
                get {
                    return connectionIssues;
                }
                internal set {
                    connectionIssues = value;
                    /*if (!value) {
                        lock (lockObject)
                        {
                            Monitor.PulseAll(camera);
                        }
                    }*/
                }
            }

            public bool HasValue { get; private set; }

            public uint Value {
                get {
                    if (!HasValue) {
                        new InvalidOperationException("No camera reference has been set.");
                    }
                    return camera;
                }
                set {
                    HasValue = true;
                    camera = value;
                }
            }
        }

        #endregion
    };
}