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
        private uint lastFrame;
        private readonly Dictionary<IntPtr, byte[]> buffers = new Dictionary<IntPtr, byte[]>();
        private readonly tFrameCallback callback;
        private long frameCount = 0;
        private bool delayFrame = false;

        public event FrameReadyHandler FrameReady;

        protected void OnFrameReady(IntPtr framePointer)
        {
            if (FrameReady != null)
            {
                if (frameCount < long.MaxValue)
                    frameCount++;
                else
                    frameCount = 0;
                tFrame tFrame = (tFrame)Marshal.PtrToStructure(framePointer, typeof(tFrame));
                Frame frame = new Frame(this, framePointer, tFrame, buffers[framePointer]);
                if (frame.FrameCount >= lastFrame)
                {
                    lastFrame = frame.FrameCount;
                    FrameReady(this, frame);
                    /*FrameReadyHandler eventHandler = FrameReady;
                    Delegate[] delegates = eventHandler.GetInvocationList();
                    foreach (FrameReadyHandler handler in delegates)
                    {
                        DispatcherObject dispatcherObject = handler.Target as DispatcherObject;
                        if (dispatcherObject != null && !dispatcherObject.CheckAccess())
                        {
                            dispatcherObject.Dispatcher.Invoke(DispatcherPriority.DataBind, handler, this, frame);
                        }
                        else
                            handler(this, frame);
                    }*/
                }
            }
        }

        internal void ReleaseFrame(IntPtr framePointer)
        {
            if(!this.camera.HasValue)
            {
                throw new PvException(tErr.eErrUnavailable);
            }
            tErr error = Pv.CaptureQueueFrame(this.camera.Value, framePointer, this.callback);
            if(error != tErr.eErrSuccess)
            {
                throw new PvException(error);
            }
        }

        public Camera(tCameraInfo cameraInfo)
        {
            this.cameraInfo = cameraInfo;
            this.callback = new tFrameCallback(OnFrameReady);
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

        public Interface InterfaceType
        {
            get { return (Interface)cameraInfo.InterfaceType; }
        }

        public string DisplayName
        {
            get { return cameraInfo.DisplayName; }
        }

        public uint Reference
        {
            get { return camera.Value; }
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
                Pv.AttrUint32Set(camera.Value, "Height", value);
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
                Pv.AttrUint32Set(camera.Value, "Width", value);
            }
        }

        public ImageFormat ImageFormat
        {
            get
            {
                ImageFormat value = 0;
                StringBuilder buffer = new StringBuilder();
                UInt32 read = 0;
                if (!camera.HasValue)
                {
                    throw new PvException(tErr.eErrUnavailable);
                }
                Pv.AttrEnumGet(camera.Value, "PixelFormat", buffer, 1000, ref read);
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

        public uint ColorCode
        {
            get
            {
                if (!camera.HasValue)
                {
                    throw new PvException(tErr.eErrUnavailable);
                }
                //TODO: Return property value.
                return 0;
            }
            set
            {
                //Todo: store value.
            }
        }

        public void BeginCapture()
        {
            tErr error;
            if (!camera.HasValue)
            {
                error = tErr.eErrUnavailable;
                throw new PvException(error);
            }
            //error = Pv.AttrEnumSet(this.camera.Value, "PixelFormat", "Rgb24");
            //if (error != tErr.eErrSuccess)
            //    goto error;
            error = Pv.CaptureStart(this.camera.Value);
            if (error != tErr.eErrSuccess)
                goto error;

            frameBufferHandles = new GCHandle[10];
            framePoolHandles = new GCHandle[10];
            frames = new tFrame[10];

            uint bufferSize = 0;
            error = Pv.AttrUint32Get(this.camera.Value, "TotalBytesPerFrame", ref bufferSize);
            if (error != tErr.eErrSuccess)
                goto error;
            byte[] buffer;
            GCHandle bufferHandle, frameHandle;
            tFrame frame;
            IntPtr framePointer;
            for (int count = 9; count >= 0; count--)
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
                if(error != tErr.eErrSuccess)
                    goto error;
            }
            error = Pv.AttrEnumSet(this.camera.Value, "AcquisitionMode", "Continuous");
            if (error != tErr.eErrSuccess)
                goto error;
            Pv.CommandRun(this.camera.Value, "AcquisitionStart");
            if (error != tErr.eErrSuccess)
                goto error;

            return;
        error:
            EndCapture();
            Pv.CaptureQueueClear(this.camera.Value);
            Pv.CommandRun(this.camera.Value, "AcquisitionStop");

            throw new PvException(error);
        }

        public void EndCapture()
        {
            if (!camera.HasValue)
            {
                throw new PvException(tErr.eErrUnavailable);
            }
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
            return camera.HasValue ? CameraSerial.WriteBytesToSerialIo(camera.Value, buffer, Convert.ToUInt32(buffer.LongLength)) : false;
        }

        public bool ReadBytesFromSerial(byte[] buffer, ref uint recieved)
        {
            return camera.HasValue ? CameraSerial.ReadBytesFromSerialIO(camera.Value, buffer, Convert.ToUInt32(buffer.LongLength), ref recieved) : false;
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
                WriteBytesToSerial(new byte[] {0x6F, 0x0D});
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