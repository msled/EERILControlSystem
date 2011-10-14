using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Drawing;
using EERIL.ControlSystem.Avt;
using EERIL.ControlSystem.Test;
using System.Windows.Threading;
using System.Text;
using System.Drawing.Imaging;
using EERIL.ControlSystem.Properties;

namespace EERIL.ControlSystem.v4 {
	[ControlSystem.Device("MsledOS", OsVersion = new double[] { 4 })]
	class Device : IDevice {
		private readonly ICamera camera;
		private string displayName = null;
		private readonly Thread serialMonitorThread;
		private readonly MemoryMappedFile file;
        private readonly EERIL.ControlSystem.Properties.Settings settings = EERIL.ControlSystem.Properties.Settings.Default;
		private List<byte> buffer = new List<byte>();
		private byte horizontalFinPosition;

		private byte verticalFinPosition;
        
        private byte topFinOffset = 0;

        private byte rightFinOffset = 0;

        private byte bottomFinOffset = 0;

        private byte leftFinOffset = 0;

		private byte thrust;

		private PowerConfigurations powerConfiguration;

		public event FrameReadyHandler FrameReady;

		public event DeviceMessageHandler MessageReceived;

	    public uint Id
	    {
            get { return camera.Reference; }
	    }

	    public string DisplayName {
			get {
				return displayName ?? camera.DisplayName;
			}
		}

        public IList<ITest> Tests
        {
            get
            {
                return new List<ITest>()
                {
                    new PowerTest(this)
                };
            }
        }

		public IList<ISensor> Sensors { get { return null; } }

		public uint ImageHeight {
			get {
                return this.camera.ImageHeight;
			}
		}

		public uint ImageWidth {
			get {
				return this.camera.ImageWidth;
			}
		}

		public uint ImageDepth {
			get {
                //Todo: return valid value;
                return 0;// camera.ImageDepth;
			}
		}

		public PixelFormat PixelFormat {
			get {
                //Todo: return valid value.
                return PixelFormat.DontCare;// camera.GrabPixelFormat();
			}
		}

		public uint ColorCode {
			get {
				return this.camera.ColorCode;
			}
		}

		public byte HorizontalFinPosition {
			get { return horizontalFinPosition; }
			set {
				if(!camera.WriteBytesToSerial(new byte[] { 0x68, value, 0x0D }))
				{
				    throw new Exception("Failed to transmit horizontal fin position to device.");
				}
				horizontalFinPosition = value;
			}
		}

		public byte VerticalFinPosition {
			get { return verticalFinPosition; }
			set {
                if (!camera.WriteBytesToSerial(new byte[] { 0x76, value, 0x0D }))
                {
                    throw new Exception("Failed to transmit vertical fin position to device.");
                }
				verticalFinPosition = value;
			}
		}

        public byte TopFinOffset
        {
            get { return topFinOffset; }
            set
            {
                if (!camera.WriteBytesToSerial(new byte[] { 0x61, 0x74, value, 0x0D }))
                {
                    throw new Exception("Failed to transmit top fin offset.");
                }
                topFinOffset = value;
            }
        }

        public byte RightFinOffset
        {
            get { return rightFinOffset; }
            set
            {
                if (!camera.WriteBytesToSerial(new byte[] { 0x61, 0x72, value, 0x0D }))
                {
                    throw new Exception("Failed to transmit right fin offset.");
                }
                rightFinOffset = value;
            }
        }

        public byte BottomFinOffset
        {
            get { return bottomFinOffset; }
            set
            {
                if (!camera.WriteBytesToSerial(new byte[] { 0x61, 0x62, value, 0x0D }))
                {
                    throw new Exception("Failed to transmit bottom fin offset.");
                }
                bottomFinOffset = value;
            }
        }

        public byte LeftFinOffset
        {
            get { return leftFinOffset; }
            set
            {
                if (!camera.WriteBytesToSerial(new byte[] { 0x61, 0x6C, value, 0x0D }))
                {
                    throw new Exception("Failed to transmit left fin offset.");
                }
                leftFinOffset = value;
            }
        }

		public byte Thrust {
			get { return thrust; }
			set {
                byte throttled = Convert.ToByte((value / 18) + 48);

                if (throttled > 52)
                    throttled--;
                else if (throttled < 52)
                    throttled++;

                if (!camera.WriteBytesToSerial(new byte[] { 0x74, throttled, 0x0D }))
                {
                    throw new Exception("Failed to transmit thrust to device.");
                }
                thrust = value;
            }
		}

		public PowerConfigurations PowerConfiguration {
			get { return powerConfiguration; }
			set { powerConfiguration = value; }
		}

		ICamera Camera {
			get {
				return camera;
			}
		}

		public Device(string displayName) {
			this.displayName = displayName;
		}

		public Device(ICamera camera) {
			this.camera = camera;
			this.camera.FrameReady += CameraFrameReady;
			file = MemoryMappedFile.CreateOrOpen(camera.SerialString + "_" + DateTime.Now,
                                                 EERIL.ControlSystem.Properties.Settings.Default.SerialMappedFileCapacity);
			serialMonitorThread = new Thread(MonitorSerialCommunication);
            serialMonitorThread.Name = "Serial Communication Monitor";
            serialMonitorThread.IsBackground = true;
            serialMonitorThread.Priority = ThreadPriority.BelowNormal;
		}

		private void CameraFrameReady(object sender, IFrame frame) {
			OnFrameReady(frame);
		}

		protected void OnFrameReady(IFrame frame) {
			if (FrameReady != null) {
				FrameReadyHandler eventHandler = FrameReady;
				Delegate[] delegates = eventHandler.GetInvocationList();
				foreach (FrameReadyHandler handler in delegates) {
					DispatcherObject dispatcherObject = handler.Target as DispatcherObject;
					if (dispatcherObject != null && !dispatcherObject.CheckAccess()) {
						dispatcherObject.Dispatcher.Invoke(DispatcherPriority.DataBind, handler, this, frame);
					} else
						handler(this, frame);
				}
			}
		}

		protected void OnMessageReceived(string message) {
			if (MessageReceived != null) {
				DeviceMessageHandler eventHandler = MessageReceived;
				Delegate[] delegates = eventHandler.GetInvocationList();
				foreach (DeviceMessageHandler handler in delegates) {
					DispatcherObject dispatcherObject = handler.Target as DispatcherObject;
					if (dispatcherObject != null && !dispatcherObject.CheckAccess()) {
						dispatcherObject.Dispatcher.Invoke(DispatcherPriority.DataBind, handler, message);
					} else
						handler(message);
				}
			}
		}

		public void StartVideoCapture(uint timeout) {
            this.camera.Open();
            this.camera.BeginCapture();
		}

		public void StopVideoCapture() {
            this.camera.EndCapture();
            this.camera.Close();
		}

		public void PrepareForGrab(ref uint dcamMode, ref uint colorCode, ref uint width, ref uint height) {
            //Todo: fix me.
			/*enUniColorCode colorCodeEn = (enUniColorCode)colorCode;
			this.camera.PrepareFreeGrab(ref dcamMode, ref colorCodeEn, ref width, ref height);
			colorCode = (uint)colorCodeEn;*/
		}

		public void GetImage(Bitmap bitmap, uint timeout) {
            //Todo: fix me.
			//this.camera.GetImage(bitmap, timeout);
		}

		private void MonitorSerialCommunication() {
			byte[] buffer = new byte[settings.SerialReceiveInputBufferSize];
			uint length = 0;
            while (true)
            {
                camera.ReadBytesFromSerial(buffer, ref length);
                if (length > 0)
                    ParseSerial(buffer, length);
				Thread.Yield();
			}
		}

		private void ParseSerial(byte[] array, uint length) {
            for (int i = 0; i < length; i++)
            {
			    if (array[i] != 0x0D)
			    {
			        buffer.Add(array[i]);
                    continue;
			    }
			    byte[] message;
                lock(buffer)
                {
                    message = buffer.ToArray(); 
                    buffer.Clear();
                }
			    OnMessageReceived(Encoding.ASCII.GetString(message));
			}
		}

        public void Open()
        {
            this.camera.Open();
            serialMonitorThread.Start();
		}

        public void Close()
        {
            serialMonitorThread.Abort();
			this.camera.Close();
		}

		#region IDisposable Members

		public void Dispose() {
			this.camera.Close();
		}

		#endregion
	}
}
