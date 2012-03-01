using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using PvNET;

namespace EERIL.ControlSystem.Avt
{
    public class Camera : ICamera
    {
        private tCameraInfo cameraInfo;
        private uint? camera;
        private GCHandle[] frameBufferHandles;
        private GCHandle[] framePoolHandles;
        private tFrame[] frames;
        private const int FRAME_POOL_SIZE = 10;
        private const int FULL_BIT_DEPTH = 12; // ADC max res. = 12 for Prosilica GC1380
        private const UInt32 MAX_PACKET_SIZE = 16456;
        private readonly Dictionary<IntPtr, byte[]> buffers = new Dictionary<IntPtr, byte[]>();
        private readonly tFrameCallback callback;
        private readonly Timer heartbeatTimer;
        private readonly byte[] heartbeat = new byte[] { 0xA6, 0x0D };

        public event FrameReadyHandler FrameReady;

        protected void OnFrameReady(IntPtr framePointer)
        {
            if (FrameReady != null)
            {
                tFrame tFrame = (tFrame)Marshal.PtrToStructure(framePointer, typeof(tFrame));
                Frame frame = new Frame(this, framePointer, tFrame, buffers[framePointer]);
                FrameReady(this, frame);
            }
        }

        internal void ReleaseFrame(IntPtr framePointer)
        {
            if (!this.camera.HasValue)
            {
                throw new PvException(tErr.eErrUnavailable);
            }
            tErr error = Pv.CaptureQueueFrame(this.camera.Value, framePointer, this.callback);
            if (error != tErr.eErrSuccess)
            {
                throw new PvException(error); // TODO: throws exception here
            }
        }

        public Camera(tCameraInfo cameraInfo)
        {
            this.heartbeatTimer = new Timer(Heartbeat, null, 100, 100);
            this.cameraInfo = cameraInfo;
            this.callback = new tFrameCallback(OnFrameReady);
        }

        public void Heartbeat(object state)
        {
            this.WriteBytesToSerial(heartbeat);
        }


        public uint UniqueId
        {
            get { return cameraInfo.UniqueId; }
        }

        public string SerialString
        {
            get { return cameraInfo.SerialString; }
        }

        public uint PartNumber
        {
            get { return cameraInfo.PartNumber; }
        }

        public uint PartVersion
        {
            get { return cameraInfo.PartVersion; }
        }

        public uint PermittedAccess
        {
            get { return cameraInfo.PermittedAccess; }
        }

        public uint InterfaceId
        {
            get { return cameraInfo.InterfaceId; }
        }

        public tInterface InterfaceType
        {
            get { return (tInterface)cameraInfo.InterfaceType; }
        }

        public string DisplayName
        {
            get { return cameraInfo.DisplayName; }
        }

        public uint Reference
        {
            get { return camera.Value; }
        }

        public float Temperature
        {
            get
            {
                float value = 0;
                if (!camera.HasValue)
                {
                    throw new PvException(tErr.eErrUnavailable);
                }
                Pv.AttrFloat32Get(camera.Value, "DeviceTemperatureMainboard", ref value);
                return value;
            }
        }

        public uint ImageHeight
        {
            get
            {
                uint value = 0;
                if (!camera.HasValue)
                {
                    throw new PvException(tErr.eErrUnavailable);
                }
                Pv.AttrUint32Get(camera.Value, "Height", ref value);
                return value;
            }
            set
            {
                if (!camera.HasValue)
                {
                    throw new PvException(tErr.eErrUnavailable);
                }
                if (value >= 1)
                {
                    Pv.AttrUint32Set(camera.Value, "Height", value);
                }
            }
        }

        public uint ImageWidth
        {
            get
            {
                uint value = 0;
                if (!camera.HasValue)
                {
                    throw new PvException(tErr.eErrUnavailable);
                }
                Pv.AttrUint32Get(camera.Value, "Width", ref value);
                return value;
            }
            set
            {
                if (!camera.HasValue)
                {
                    throw new PvException(tErr.eErrUnavailable);
                }
                if (value >= 1)
                {
                    Pv.AttrUint32Set(camera.Value, "Width", value);
                }
            }
        }

        public uint ImageDepth
        {
            get
            {
                uint value = 0;
                switch (ImageFormat)
                {
                    case tImageFormat.eFmtMono8:
                    case tImageFormat.eFmtBayer8:
                    case tImageFormat.eFmtRgb24:
                    case tImageFormat.eFmtBgr24:
                    case tImageFormat.eFmtYuv411:
                    case tImageFormat.eFmtYuv422:
                    case tImageFormat.eFmtYuv444:
                    case tImageFormat.eFmtRgba32:
                    case tImageFormat.eFmtBgra32:
                        value = 8;
                        break;

                    case tImageFormat.eFmtMono12Packed:
                    case tImageFormat.eFmtBayer12Packed:
                        value = 12;
                        break;

                    case tImageFormat.eFmtMono16:
                    case tImageFormat.eFmtBayer16:
                    case tImageFormat.eFmtRgb48:
                        value = FULL_BIT_DEPTH; // depends on hardware
                        break;

                    default:
                        value = 0;
                        break;
                }
                return value;
            }
        }

        public float BytesPerPixel
        {
            get
            {
                float value = 0;
                switch (ImageFormat)
                {
                    case tImageFormat.eFmtMono8:
                    case tImageFormat.eFmtBayer8:
                        value = 1;
                        break;

                    case tImageFormat.eFmtYuv411:
                    case tImageFormat.eFmtMono12Packed:
                    case tImageFormat.eFmtBayer12Packed:
                        value = 1;
                        break;

                    case tImageFormat.eFmtMono16:
                    case tImageFormat.eFmtBayer16:
                    case tImageFormat.eFmtYuv422:
                        value = 2;
                        break;

                    case tImageFormat.eFmtRgb24:
                    case tImageFormat.eFmtBgr24:
                    case tImageFormat.eFmtYuv444:
                        value = 3;
                        break;

                    case tImageFormat.eFmtRgba32:
                    case tImageFormat.eFmtBgra32:
                        value = 4;
                        break;

                    case tImageFormat.eFmtRgb48:
                        value = 6;
                        break;

                    default:
                        value = 0;
                        break;
                }
                return value;
            }
        }

        public tImageFormat ImageFormat
        {
            get
            {
                tImageFormat value = 0;
                StringBuilder buffer = new StringBuilder(16);
                uint read = 0;
                if (!camera.HasValue)
                {
                    throw new PvException(tErr.eErrUnavailable);
                }
                Pv.AttrEnumGet(camera.Value, "PixelFormat", buffer, 16, ref read);
                value = (tImageFormat)Enum.Parse(typeof(tImageFormat), buffer.ToString().Substring(4, buffer.Length - 4), true); // parse without "eFmt" in positions 0-3
                return value;
            }
            set
            {
                if (!camera.HasValue)
                {
                    throw new PvException(tErr.eErrUnavailable);
                }
                Pv.AttrUint32Set(camera.Value, "PixelFormat", (uint)value);
            }
        }

        public ColorTransformation ColorTransformation
        {
            get
            {
                ColorTransformation value = new ColorTransformation();
                StringBuilder buffer = new StringBuilder(16);
                uint read = 0;
                if (!camera.HasValue)
                {
                    throw new PvException(tErr.eErrUnavailable);
                }
                Pv.AttrEnumGet(camera.Value, "ColorTransformationMode", buffer, 16, ref read);
                value.mode = (ColorTransformationMode)Enum.Parse(typeof(ColorTransformationMode), buffer.ToString(), true);
                Pv.AttrFloat32Get(camera.Value, "ColorTransformationValueRR", ref value.values[0][0]);
                Pv.AttrFloat32Get(camera.Value, "ColorTransformationValueRG", ref value.values[0][1]);
                Pv.AttrFloat32Get(camera.Value, "ColorTransformationValueRB", ref value.values[0][2]);
                Pv.AttrFloat32Get(camera.Value, "ColorTransformationValueGR", ref value.values[1][0]);
                Pv.AttrFloat32Get(camera.Value, "ColorTransformationValueGG", ref value.values[1][1]);
                Pv.AttrFloat32Get(camera.Value, "ColorTransformationValueGB", ref value.values[1][2]);
                Pv.AttrFloat32Get(camera.Value, "ColorTransformationValueBR", ref value.values[2][0]);
                Pv.AttrFloat32Get(camera.Value, "ColorTransformationValueBG", ref value.values[2][1]);
                Pv.AttrFloat32Get(camera.Value, "ColorTransformationValueBB", ref value.values[2][2]);
                return value;
            }
            set
            {
                if (!camera.HasValue)
                {
                    throw new PvException(tErr.eErrUnavailable);
                }
                Pv.AttrUint32Set(camera.Value, "ColorTransformationMode", (uint)value.mode);
                if (value.values[0][0] >= 0.0 && value.values[0][0] <= 2.0)
                {
                    Pv.AttrFloat32Set(camera.Value, "ColorTransformationValueRR", value.values[0][0]);
                }
                if (value.values[0][1] >= 0.0 && value.values[0][1] <= 2.0)
                {
                    Pv.AttrFloat32Set(camera.Value, "ColorTransformationValueRG", value.values[0][1]);
                }
                if (value.values[0][2] >= 0.0 && value.values[0][2] <= 2.0)
                {
                    Pv.AttrFloat32Set(camera.Value, "ColorTransformationValueRB", value.values[0][2]);
                }
                if (value.values[1][0] >= 0.0 && value.values[1][0] <= 2.0)
                {
                    Pv.AttrFloat32Set(camera.Value, "ColorTransformationValueGR", value.values[1][0]);
                }
                if (value.values[1][1] >= 0.0 && value.values[1][1] <= 2.0)
                {
                    Pv.AttrFloat32Set(camera.Value, "ColorTransformationValueGG", value.values[1][1]);
                }
                if (value.values[1][2] >= 0.0 && value.values[1][2] <= 2.0)
                {
                    Pv.AttrFloat32Set(camera.Value, "ColorTransformationValueGB", value.values[1][2]);
                }
                if (value.values[2][0] >= 0.0 && value.values[2][0] <= 2.0)
                {
                    Pv.AttrFloat32Set(camera.Value, "ColorTransformationValueBR", value.values[2][0]);
                }
                if (value.values[2][1] >= 0.0 && value.values[2][1] <= 2.0)
                {
                    Pv.AttrFloat32Set(camera.Value, "ColorTransformationValueBG", value.values[2][1]);
                }
                if (value.values[2][2] >= 0.0 && value.values[2][2] <= 2.0)
                {
                    Pv.AttrFloat32Set(camera.Value, "ColorTransformationValueBB", value.values[2][2]);
                }
            }
        }

        public DSP DSP
        {
            get
            {
                DSP value = new DSP();
                if (!camera.HasValue)
                {
                    throw new PvException(tErr.eErrUnavailable);
                }
                Pv.AttrUint32Get(camera.Value, "DSPSubregionBottom", ref value.bottom);
                Pv.AttrUint32Get(camera.Value, "DSPSubregionLeft", ref value.left);
                Pv.AttrUint32Get(camera.Value, "DSPSubregionRight", ref value.right);
                Pv.AttrUint32Get(camera.Value, "DSPSubregionTop", ref value.top);
                return value;
            }
            set
            {
                if (!camera.HasValue)
                {
                    throw new PvException(tErr.eErrUnavailable);
                }
                if (value.bottom >= 0 && value.bottom <= 4294967295)
                {
                    Pv.AttrUint32Set(camera.Value, "DSPSubregionBottom", value.bottom);
                }
                if (value.bottom >= 0 && value.bottom <= 4294967295)
                {
                    Pv.AttrUint32Set(camera.Value, "DSPSubregionLeft", value.left);
                }
                if (value.bottom >= 0 && value.bottom <= 4294967295)
                {
                    Pv.AttrUint32Set(camera.Value, "DSPSubregionRight", value.right);
                }
                if (value.bottom >= 0 && value.bottom <= 4294967295)
                {
                    Pv.AttrUint32Set(camera.Value, "DSPSubregionTop", value.top);
                }
            }
        }

        public EdgeFilter EdgeFilter
        {
            get
            {
                EdgeFilter value = EdgeFilter.Off;
                StringBuilder buffer = new StringBuilder(16);
                uint read = 0;
                if (!camera.HasValue)
                {
                    throw new PvException(tErr.eErrUnavailable);
                }
                Pv.AttrEnumGet(camera.Value, "EdgeFilter", buffer, 16, ref read);
                value = (EdgeFilter)Enum.Parse(typeof(EdgeFilter), buffer.ToString(), true);
                return value;
            }
            set
            {
                if (!camera.HasValue)
                {
                    throw new PvException(tErr.eErrUnavailable);
                }
                Pv.AttrEnumSet(camera.Value, "EdgeFilter", value.ToString());
            }
        }

        public float Gamma
        {
            get
            {
                float value = 0;
                if (!camera.HasValue)
                {
                    throw new PvException(tErr.eErrUnavailable);
                }
                Pv.AttrFloat32Get(camera.Value, "Gamma", ref value);
                return value;
            }
            set
            {
                if (!camera.HasValue)
                {
                    throw new PvException(tErr.eErrUnavailable);
                }
                if (value >= 0.25 && value <= 3.0)
                {
                    Pv.AttrFloat32Set(camera.Value, "Gamma", value);
                }
            }
        }

        public float Hue
        {
            get
            {
                float value = 0;
                if (!camera.HasValue)
                {
                    throw new PvException(tErr.eErrUnavailable);
                }
                Pv.AttrFloat32Get(camera.Value, "Hue", ref value);
                return value;
            }
            set
            {
                if (!camera.HasValue)
                {
                    throw new PvException(tErr.eErrUnavailable);
                }
                if (value >= -40.0 && value <= 40.0)
                {
                    Pv.AttrFloat32Set(camera.Value, "Hue", value);
                }
            }
        }

        public Gain Gain
        {
            get
            {
                StringBuilder buffer = new StringBuilder(16);
                Gain value = new Gain();
                uint read = 0;
                if (!camera.HasValue)
                {
                    throw new PvException(tErr.eErrUnavailable);
                }

                Pv.AttrEnumGet(camera.Value, "GainMode", buffer, 16, ref read);
                value.mode = (GainMode)Enum.Parse(typeof(GainMode), buffer.ToString(), true);
                Pv.AttrUint32Get(camera.Value, "GainAutoAdjustTol", ref value.tolerance);
                Pv.AttrUint32Get(camera.Value, "GainAutoMax", ref value.max);
                Pv.AttrUint32Get(camera.Value, "GainAutoMin", ref value.min);
                Pv.AttrUint32Get(camera.Value, "GainAutoOutliers", ref value.outliers);
                Pv.AttrUint32Get(camera.Value, "GainAutoRate", ref value.rate);
                Pv.AttrUint32Get(camera.Value, "GainAutoTarget", ref value.target);
                Pv.AttrUint32Get(camera.Value, "GainValue", ref value.value);
                return value;
            }
            set
            {
                if (!camera.HasValue)
                {
                    throw new PvException(tErr.eErrUnavailable);
                }

                Pv.AttrEnumSet(camera.Value, "GainMode", value.mode.ToString());
                if (value.tolerance >= 0 && value.tolerance <= 50)
                {
                    Pv.AttrUint32Set(camera.Value, "GainAutoAdjustTol", value.tolerance);
                }
                if (value.max >= 0)
                {
                    Pv.AttrUint32Set(camera.Value, "GainAutoMax", value.max);
                }
                if (value.min >= 0)
                {
                    Pv.AttrUint32Set(camera.Value, "GainAutoMin", value.min);
                }
                if (value.outliers >= 0 && value.outliers <= 1000)
                {
                    Pv.AttrUint32Set(camera.Value, "GainAutoOutliers", value.outliers);
                }
                if (value.rate >= 1 && value.rate <= 100)
                {
                    Pv.AttrUint32Set(camera.Value, "GainAutoRate", value.rate);
                }
                if (value.target >= 0 && value.target <= 100)
                {
                    Pv.AttrUint32Set(camera.Value, "GainAutoTarget", value.target);
                }
                if (value.value >= 0)
                {
                    Pv.AttrUint32Set(camera.Value, "GainValue", value.value);
                }
            }
        }

        public Exposure Exposure
        {
            get
            {
                StringBuilder buffer = new StringBuilder(16);
                Exposure value = new Exposure();
                uint read = 0;
                if (!camera.HasValue)
                {
                    throw new PvException(tErr.eErrUnavailable);
                }

                Pv.AttrEnumGet(camera.Value, "ExposureAutoAlg", buffer, 16, ref read);
                value.algorithm = (ExposureAlgorithm)Enum.Parse(typeof(ExposureAlgorithm), buffer.ToString(), true);
                buffer.Clear();
                Pv.AttrEnumGet(camera.Value, "ExposureMode", buffer, 16, ref read);
                value.mode = (ExposureMode)Enum.Parse(typeof(ExposureMode), buffer.ToString(), true);
                Pv.AttrUint32Get(camera.Value, "ExposureAutoAdjustTol", ref value.tolerance);
                Pv.AttrUint32Get(camera.Value, "ExposureAutoMax", ref value.max);
                Pv.AttrUint32Get(camera.Value, "ExposureAutoMin", ref value.min);
                Pv.AttrUint32Get(camera.Value, "ExposureAutoOutliers", ref value.outliers);
                Pv.AttrUint32Get(camera.Value, "ExposureAutoRate", ref value.rate);
                Pv.AttrUint32Get(camera.Value, "ExposureAutoTarget", ref value.target);
                Pv.AttrUint32Get(camera.Value, "ExposureValue", ref value.value);
                return value;
            }
            set
            {
                if (!camera.HasValue)
                {
                    throw new PvException(tErr.eErrUnavailable);
                }

                Pv.AttrEnumSet(camera.Value, "ExposureAutoAlg", value.algorithm.ToString());
                Pv.AttrEnumSet(camera.Value, "ExposureMode", value.mode.ToString());
                if (value.tolerance >= 0 && value.tolerance <= 50)
                {
                    Pv.AttrUint32Set(camera.Value, "ExposureAutoAdjustTol", value.tolerance);
                }
                if (value.max >= 0 && value.max <= 60000000)
                {
                    Pv.AttrUint32Set(camera.Value, "ExposureAutoMax", value.max);
                }
                if (value.min >= 0 && value.min <= 60000000)
                {
                    Pv.AttrUint32Set(camera.Value, "ExposureAutoMin", value.min);
                }
                if (value.outliers >= 0 && value.outliers <= 1000)
                {
                    Pv.AttrUint32Set(camera.Value, "ExposureAutoOutliers", value.outliers);
                }
                if (value.rate >= 1 && value.rate <= 100)
                {
                    Pv.AttrUint32Set(camera.Value, "ExposureAutoRate", value.rate);
                }
                if (value.target >= 0 && value.target <= 100)
                {
                    Pv.AttrUint32Set(camera.Value, "ExposureAutoTarget", value.target);
                }
                if (value.value >= 0 && value.value <= 60000000)
                {
                    Pv.AttrUint32Set(camera.Value, "ExposureValue", value.value);
                }
            }
        }

        public void BeginCapture(tImageFormat fmt)
        {
            tErr error;
            if (!camera.HasValue)
            {
                error = tErr.eErrUnavailable;
                throw new PvException(error);
            }

            this.ImageFormat = fmt;

            error = Pv.CaptureStart(this.camera.Value);
            if (error != tErr.eErrSuccess)
                goto error;

            frameBufferHandles = new GCHandle[FRAME_POOL_SIZE];
            framePoolHandles = new GCHandle[FRAME_POOL_SIZE];
            frames = new tFrame[FRAME_POOL_SIZE];

            uint bufferSize = 0;
            error = Pv.AttrUint32Get(this.camera.Value, "TotalBytesPerFrame", ref bufferSize);
            if (error != tErr.eErrSuccess)
                goto error;
            byte[] buffer;
            GCHandle bufferHandle, frameHandle;
            tFrame frame;
            IntPtr framePointer;
            for (int count = FRAME_POOL_SIZE - 1; count >= 0; count--)
            {
                buffer = new byte[bufferSize];
                bufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                frameBufferHandles[count] = bufferHandle;
                frame = new tFrame
                    {
                        ImageBuffer = bufferHandle.AddrOfPinnedObject(),
                        ImageBufferSize = bufferSize,
                        AncillaryBufferSize = 0
                    };
                frames[count] = frame;
                frameHandle = GCHandle.Alloc(frame, GCHandleType.Pinned);
                framePoolHandles[count] = frameHandle;
                framePointer = frameHandle.AddrOfPinnedObject();
                buffers.Add(framePointer, buffer);
                error = Pv.CaptureQueueFrame(this.camera.Value, framePointer, this.callback);
                if (error != tErr.eErrSuccess)
                    goto error;
            }
            error = Pv.AttrFloat32Set(this.camera.Value, "FrameRate", 15);
            if (error != tErr.eErrSuccess)
                goto error;
            error = Pv.AttrEnumSet(this.camera.Value, "FrameStartTriggerMode", "FixedRate");
            if (error != tErr.eErrSuccess)
                goto error;
            error = Pv.AttrEnumSet(this.camera.Value, "AcquisitionMode", "Continuous");
            if (error != tErr.eErrSuccess)
                goto error;
            error = Pv.CommandRun(this.camera.Value, "AcquisitionStart");
            if (error != tErr.eErrSuccess)
                goto error;
            return;
        error:
            EndCapture();

            throw new PvException(error);
        }

        public void EndCapture()
        {
            if (!camera.HasValue)
            {
                throw new PvException(tErr.eErrUnavailable);
            }
            Pv.CaptureQueueClear(this.camera.Value);
            foreach (GCHandle handle in framePoolHandles)
            {
                handle.Free();
            }
            foreach (GCHandle handle in frameBufferHandles)
            {
                handle.Free();
            }
            frames = null;
            Pv.CommandRun(this.camera.Value, "AcquisitionStop");
            Pv.CaptureEnd(this.camera.Value);
        }

        public bool WriteBytesToSerial(byte[] buffer)
        {
            heartbeatTimer.Change(100, 100);
            return camera.HasValue && CameraSerial.WriteBytesToSerialIo(camera.Value, buffer, Convert.ToUInt32(buffer.LongLength));
        }

        public bool ReadBytesFromSerial(byte[] buffer, ref uint recieved)
        {
            return camera.HasValue && CameraSerial.ReadBytesFromSerialIO(camera.Value, buffer, Convert.ToUInt32(buffer.LongLength), ref recieved);
        }

        public void Open()
        {
            if (!camera.HasValue)
            {
                uint cameraId;
                Pv.CameraClose(cameraInfo.UniqueId);
                tErr err = Pv.CameraOpen(cameraInfo.UniqueId, tAccessFlags.eAccessMaster, out cameraId);
                if (err != tErr.eErrSuccess)
                {
                    throw new PvException(err);
                }
                camera = cameraId;
                CameraSerial.Setup(cameraId);
                WriteBytesToSerial(new byte[] { 0x6F, 0x0D });
            }
        }

        public void AdjustPacketSize()
        {
            if (!camera.HasValue)
            {
                throw new PvException(tErr.eErrUnavailable);
            }
            tErr err = Pv.CaptureAdjustPacketSize(cameraInfo.UniqueId, MAX_PACKET_SIZE);
            if (err != tErr.eErrSuccess)
            {
                throw new PvException(err);
            }
        }

        public void Close()
        {
            if (camera.HasValue)
            {
                tErr err = Pv.CameraClose(camera.Value);
                camera = null;
                if (err != tErr.eErrSuccess)
                {
                    throw new PvException(err);
                }
            }
        }
    };
}