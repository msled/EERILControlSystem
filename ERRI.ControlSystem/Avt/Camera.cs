using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using EERIL.ControlSystem.Communication;
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
        private const UInt32 MAX_PACKET_SIZE = 16456;
        private readonly Dictionary<IntPtr, byte[]> buffers = new Dictionary<IntPtr, byte[]>();
        private readonly tFrameCallback callback;
        private CommunicationsManager communicationManager;

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
            communicationManager.QueueFrame(framePointer, callback);
        }

        public Camera(tCameraInfo cameraInfo)
        {
            this.cameraInfo = cameraInfo;
            callback = OnFrameReady;
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
            get { return cameraInfo.InterfaceType; }
        }

        public string DisplayName
        {
            get { return cameraInfo.DisplayName; }
        }

        public uint Reference
        {
            get { return camera.Value; }
        }

        public float FrameRate
        {
            get
            {
                float value = 0;
                if (!camera.HasValue)
                {
                    throw new PvException(tErr.eErrUnavailable);
                }
                Pv.AttrFloat32Get(camera.Value, "FrameRate", ref value);
                return value;
            }
            set
            {
                if (!camera.HasValue)
                {
                    throw new PvException(tErr.eErrUnavailable);
                }
                Pv.AttrFloat32Set(camera.Value, "FrameRate", value);
            }
        }

        public ImageFormat ImageFormat
        {
            get
            {
                ImageFormat value = new ImageFormat();
                StringBuilder buffer = new StringBuilder(16);
                uint read = 0;
                if (!camera.HasValue)
                {
                    throw new PvException(tErr.eErrUnavailable);
                }
                Pv.AttrEnumGet(camera.Value, "PixelFormat", buffer, 16, ref read);
                value.pixelformat = (tImageFormat)Enum.Parse(typeof(tImageFormat), buffer.Insert(0, "eFmt").ToString(), true); // prepend "eFmt" in positions 0-3
                Pv.AttrUint32Get(camera.Value, "Height", ref value.height);
                Pv.AttrUint32Get(camera.Value, "RegionX", ref value.regionx);
                Pv.AttrUint32Get(camera.Value, "RegionY", ref value.regiony);
                Pv.AttrUint32Get(camera.Value, "Width", ref value.width);
                Pv.AttrUint32Get(camera.Value, "TotalBytesPerFrame", ref value.bytesperframe);
                return value;
            }
            set
            {
                if (!camera.HasValue)
                {
                    throw new PvException(tErr.eErrUnavailable);
                }
                Pv.AttrEnumSet(camera.Value, "PixelFormat", value.pixelformat.ToString().Substring(4)); // remove "eFmt" in position 0-3
                if (value.height >= 1)
                {
                    Pv.AttrUint32Set(camera.Value, "Height", value.height);
                }
                if (value.width >= 1)
                {
                    Pv.AttrUint32Set(camera.Value, "Width", value.width);
                }
                Pv.AttrUint32Set(camera.Value, "RegionX", value.regionx);
                Pv.AttrUint32Set(camera.Value, "RegionY", value.regiony);
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
                if (value.bottom <= 4294967295)
                {
                    Pv.AttrUint32Set(camera.Value, "DSPSubregionBottom", value.bottom);
                }
                if (value.bottom <= 4294967295)
                {
                    Pv.AttrUint32Set(camera.Value, "DSPSubregionLeft", value.left);
                }
                if (value.bottom <= 4294967295)
                {
                    Pv.AttrUint32Set(camera.Value, "DSPSubregionRight", value.right);
                }
                if (value.bottom <= 4294967295)
                {
                    Pv.AttrUint32Set(camera.Value, "DSPSubregionTop", value.top);
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
                if (value.tolerance <= 50)
                {
                    Pv.AttrUint32Set(camera.Value, "GainAutoAdjustTol", value.tolerance);
                }
                if (value.outliers <= 1000)
                {
                    Pv.AttrUint32Set(camera.Value, "GainAutoOutliers", value.outliers);
                }
                if (value.rate >= 1 && value.rate <= 100)
                {
                    Pv.AttrUint32Set(camera.Value, "GainAutoRate", value.rate);
                }
                if (value.target <= 100)
                {
                    Pv.AttrUint32Set(camera.Value, "GainAutoTarget", value.target);
                }
                Pv.AttrUint32Set(camera.Value, "GainValue", value.value);
                Pv.AttrUint32Set(camera.Value, "GainAutoMax", value.max);
                Pv.AttrUint32Set(camera.Value, "GainAutoMin", value.min);
            }
        }

        public WhiteBalance WhiteBalance
        {
            get
            {
                StringBuilder buffer = new StringBuilder(16);
                WhiteBalance value = new WhiteBalance();
                uint read = 0;
                if (!camera.HasValue)
                {
                    throw new PvException(tErr.eErrUnavailable);
                }

                Pv.AttrEnumGet(camera.Value, "WhitebalMode", buffer, 16, ref read);
                value.mode = (WhiteBalanceMode)Enum.Parse(typeof(WhiteBalanceMode), buffer.ToString(), true);
                Pv.AttrUint32Get(camera.Value, "WhitebalAutoAdjustTol", ref value.tolerance);
                Pv.AttrUint32Get(camera.Value, "WhitebalAutoRate", ref value.rate);
                Pv.AttrUint32Get(camera.Value, "WhitebalValueRed", ref value.red);
                Pv.AttrUint32Get(camera.Value, "WhitebalValueBlue", ref value.blue);
                return value;
            }
            set
            {
                if (!camera.HasValue)
                {
                    throw new PvException(tErr.eErrUnavailable);
                }

                Pv.AttrEnumSet(camera.Value, "WhitebalMode", value.mode.ToString());
                if (value.tolerance <= 50)
                {
                    Pv.AttrUint32Set(camera.Value, "WhitebalAutoAdjustTol", value.tolerance);
                }
                if (value.rate >= 1 && value.rate <= 100)
                {
                    Pv.AttrUint32Set(camera.Value, "WhitebalAutoRate", value.rate);
                }
                Pv.AttrUint32Set(camera.Value, "WhitebalValueRed", value.red);
                Pv.AttrUint32Set(camera.Value, "WhitebalValueBlue", value.blue);
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
                if (value.tolerance <= 50)
                {
                    Pv.AttrUint32Set(camera.Value, "ExposureAutoAdjustTol", value.tolerance);
                }
                if (value.max <= 60000000)
                {
                    Pv.AttrUint32Set(camera.Value, "ExposureAutoMax", value.max);
                }
                if (value.min <= 60000000)
                {
                    Pv.AttrUint32Set(camera.Value, "ExposureAutoMin", value.min);
                }
                if (value.outliers <= 1000)
                {
                    Pv.AttrUint32Set(camera.Value, "ExposureAutoOutliers", value.outliers);
                }
                if (value.rate >= 1 && value.rate <= 100)
                {
                    Pv.AttrUint32Set(camera.Value, "ExposureAutoRate", value.rate);
                }
                if (value.target <= 100)
                {
                    Pv.AttrUint32Set(camera.Value, "ExposureAutoTarget", value.target);
                }
                if (value.value <= 60000000)
                {
                    Pv.AttrUint32Set(camera.Value, "ExposureValue", value.value);
                }
            }
        }

        public ICommunicationsManager CommunicationManager {
            get {
                return communicationManager;
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

            ImageFormat.pixelformat = fmt;

            error = Pv.CaptureStart(camera.Value);
            if (error != tErr.eErrSuccess)
                goto error;

            frameBufferHandles = new GCHandle[FRAME_POOL_SIZE];
            framePoolHandles = new GCHandle[FRAME_POOL_SIZE];
            frames = new tFrame[FRAME_POOL_SIZE];

            uint bufferSize = 0;
            error = Pv.AttrUint32Get(camera.Value, "TotalBytesPerFrame", ref bufferSize);
            if (error != tErr.eErrSuccess)
                goto error;
            for (int count = FRAME_POOL_SIZE - 1; count >= 0; count--)
            {
                byte[] buffer = new byte[bufferSize];
                GCHandle bufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                frameBufferHandles[count] = bufferHandle;
                tFrame frame = new tFrame
                {
                    ImageBuffer = bufferHandle.AddrOfPinnedObject(),
                    ImageBufferSize = bufferSize,
                    AncillaryBufferSize = 0
                };
                frames[count] = frame;
                GCHandle frameHandle = GCHandle.Alloc(frame, GCHandleType.Pinned);
                framePoolHandles[count] = frameHandle;
                IntPtr framePointer = frameHandle.AddrOfPinnedObject();
                buffers.Add(framePointer, buffer);
                if (!communicationManager.QueueFrame(framePointer, callback))
                    goto error;
            }
            this.FrameRate = 15;
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
                communicationManager = new CommunicationsManager(cameraId);
                CommunicationManager.TransmitCommand(new ValuelessCommand((byte)CommandCode.Initialize));
            }
        }

        public void AdjustPacketSize()
        {
            if (!camera.HasValue)
            {
                throw new PvException(tErr.eErrUnavailable);
            }
            tErr err = Pv.CaptureAdjustPacketSize(camera.Value, MAX_PACKET_SIZE);
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

            if (CommunicationManager != null) {
                CommunicationManager.Dispose();
            }
        }
    };
}