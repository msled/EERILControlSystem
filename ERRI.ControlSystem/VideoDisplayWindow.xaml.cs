using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Drawing;
using EERIL.ControlSystem.Avt;
using EERIL.ControlSystem.Properties;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Threading;
using EERIL.DeviceControls;

namespace EERIL.ControlSystem {
    public delegate void BitmapFrameCapturedHandler(BitmapFrame frame);
	/// <summary>
	/// Interaction logic for VideoDisplay.xaml
	/// </summary>
	public partial class VideoDisplayWindow
	{
        public event BitmapFrameCapturedHandler BitmapFrameCaptured;
		private readonly IDeviceManager deviceManager;
	    private const byte axisAngleDivisor = byte.MaxValue/180;
		private Controller controller;
        [return: MarshalAs(UnmanagedType.U1)]
		[DllImport("gdi32.dll")]
		protected static extern bool DeleteObject(IntPtr hObject);
        private List<byte> sent = new List<byte>(100);
        private bool captureFrame = false;
        private BitmapFrame bitmapFrame = null;
        private readonly TriggerStateChangedHandler triggerStateChangedHandler;
        private readonly ButtonStateChangedHandler buttonStateChangedHandler;
        private byte[] m11Buffer = new byte[4], 
            m12Buffer = new byte[4], 
            m13Buffer = new byte[4], 
            m23Buffer = new byte[4], 
            m33Buffer = new byte[4],
            batteryBuffer = new byte[2];
		public DashboardWindow Dashboard {
			get;
			set;
		}

		private IDeployment Deployment {
			get;
			set;
		}

		private IMission Mission {
			get;
			set;
		}

        public double YawOffset
        {
            get
            {
                return headsUpDisplay.YawOffset;
            }
            set
            {
                headsUpDisplay.YawOffset = value;
            }
        }

        public Controller Controller
        {
            get
            {
                return controller;
            }
            set
            {
                if (controller != null)
                {
                    controller.TriggerStateChanged -= triggerStateChangedHandler;
                    controller.ButtonStateChanged -= buttonStateChangedHandler;
                }
                controller = value;
                if (controller != null)
                {
                    controller.TriggerStateChanged += triggerStateChangedHandler;
                    controller.ButtonStateChanged += buttonStateChangedHandler;
                }
            }
        }

        private void OnBitmapFrameCaptured(BitmapFrame frame)
        {
            if (BitmapFrameCaptured != null)
            {
                BitmapFrameCapturedHandler eventHandler = BitmapFrameCaptured;
                Delegate[] delegates = eventHandler.GetInvocationList();
                foreach (BitmapFrameCapturedHandler handler in delegates)
                {
                    DispatcherObject dispatcherObject = handler.Target as DispatcherObject;
                    if (dispatcherObject != null && !dispatcherObject.CheckAccess())
                    {
                        dispatcherObject.Dispatcher.Invoke(DispatcherPriority.DataBind, handler, frame);
                    }
                    else
                        handler(frame);
                }
            }
        }

		public VideoDisplayWindow(IMission mission, IDeployment deployment) {
			InitializeComponent();
			Mission = mission;
			Deployment = deployment;
			Title = String.Format("Video - {0} > {1}", mission.Name, deployment.DateTime.ToString());
		    var app = Application.Current as App;
		    if (app == null)
		    {
		       throw new Exception("Something has gone arye!"); 
		    }

            triggerStateChangedHandler = new TriggerStateChangedHandler(ControllerTriggerStateChanged);
            buttonStateChangedHandler = new ButtonStateChangedHandler(ControllerButtonStateChanged);

            deviceManager = (Application.Current as App).DeviceManager;
            if (deployment.Devices.Count > 0)
            {
                deviceManager.ActiveDevice = deployment.Devices[0];
                deviceManager.ActiveDevice.MessageReceived += new DeviceMessageHandler(ActiveDeviceMessageReceived);
            }
		    this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
		                                                                                                             {
				if (deployment.Devices.Count > 0) {
					IDevice device = deployment.Devices[0];
					device.FrameReady += DeviceFrameReady;
                    device.Open();
                    try
                    {
					    deviceManager.ActiveDevice.StartVideoCapture(1000);
                    } catch (Exception ex)
                    {
                        StringBuilder message = new StringBuilder(ex.ToString());
                        Exception inner = ex.InnerException;
                        while(inner != null)
                        {
                            message.AppendLine(inner.ToString());
                            inner = inner.InnerException;
                        }
                        MessageBox.Show(message.ToString(), "Error Initializing Capture:" + ex.Message);
                        this.Close();
                    }
                }
				return null;
			}), null);
		}

        void ControllerTriggerStateChanged(Trigger trigger, bool pressed)
        {
            switch (trigger)
            {
                case Trigger.Left:
                    if (pressed)
                    {
                        captureFrame = true;
                    }
                    break;
                case Trigger.Right:
                    deviceManager.ActiveDevice.Turbo = pressed;
                    break;
            }
        }

		void ControllerButtonStateChanged(Button button, bool pressed) {
            switch (button)
            {
                case Button.Y:
                    if (pressed)
                    {
                        headsUpDisplay.Visibility = headsUpDisplay.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
                    }
                    break;
            }
		}

        void ActiveDeviceMessageReceived(object sender, byte[] message)
        {
            switch (message[0])
            {
                case 0x74:
                    if (message.Length < 2)
                        break;
                    headsUpDisplay.Thrust = message[1] - 90;
                    break;
                case 0x62:
                    if (message.Length < 5)
                        break;
                    headsUpDisplay.Current = BitConverter.ToUInt16(message, 1);
                    headsUpDisplay.Voltage = BitConverter.ToUInt16(message, 3);
                    break;
                case 0xCC:
                    if (!verifyChecksum(message))
                        break;
                    uint timer = BitConverter.ToUInt32(message, 73);
                    /*headsUpDisplay.Acceleration = new Point3D(){
                        X = BitConverter.ToSingle(message, 1),
                        Y = BitConverter.ToSingle(message, 4),
                        Z = BitConverter.ToSingle(message, 9)
                    };
                    headsUpDisplay.AngleRate = new Point3D(){
                        X = BitConverter.ToSingle(message, 13),
                        Y = BitConverter.ToSingle(message, 17),
                        Z = BitConverter.ToSingle(message, 21)
                    };
                    headsUpDisplay.Magnetometer = new Point3D(){
                        X = BitConverter.ToSingle(message, 25),
                        Y = BitConverter.ToSingle(message, 29),
                        Z = BitConverter.ToSingle(message, 33)
                    };
                    headsUpDisplay.Orientation = new OrientationMatrix()
                    {
                        M11 = BitConverter.ToSingle(message, 37),
                        M12 = BitConverter.ToSingle(message, 41),
                        M13 = BitConverter.ToSingle(message, 45),
                        M21 = BitConverter.ToSingle(message, 49),
                        M22 = BitConverter.ToSingle(message, 53),
                        M23 = BitConverter.ToSingle(message, 57),
                        M31 = BitConverter.ToSingle(message, 61),
                        M32 = BitConverter.ToSingle(message, 65),
                        M33 = BitConverter.ToSingle(message, 69)
                    };*/
                    
                    m11Buffer[0] = message[40];
                    m11Buffer[1] = message[39];
                    m11Buffer[2] = message[38];
                    m11Buffer[3] = message[37];

                    m12Buffer[0] = message[44];
                    m12Buffer[1] = message[43];
                    m12Buffer[2] = message[42];
                    m12Buffer[3] = message[41];
                    //This is wrong :). Because of the direction the sensor is mounted, we have to invert these values. Again, this is backward.
                    headsUpDisplay.Yaw = Math.Atan2(BitConverter.ToSingle(m11Buffer, 0), BitConverter.ToSingle(m12Buffer, 0));
                    m13Buffer[0] = message[48];
                    m13Buffer[1] = message[47];
                    m13Buffer[2] = message[46];
                    m13Buffer[3] = message[45];
                    //This is wrong :). Because of the direction the sensor is mounted, we have to invert these values. Again, this is backward.
                    headsUpDisplay.Pitch = Math.Asin(BitConverter.ToSingle(m13Buffer, 0) * -1) * -1;
                    m23Buffer[0] = message[60];
                    m23Buffer[1] = message[59];
                    m23Buffer[2] = message[58];
                    m23Buffer[3] = message[57];

                    m33Buffer[0] = message[72];
                    m33Buffer[1] = message[71];
                    m33Buffer[2] = message[70];
                    m33Buffer[3] = message[69];
                    //This is wrong :). Because of the direction the sensor is mounted, we have to invert these values. Again, this is backward.
                    headsUpDisplay.Roll = Math.Atan2(BitConverter.ToSingle(m23Buffer, 0), BitConverter.ToSingle(m33Buffer, 0));
                    //Did I mention ^ that stuff is backwards.
                    break;
            }
        }

        private bool verifyChecksum(byte[] message){
            bool result = false;
            if (message.Length > 2)
            {
                int checksum = 0, messageChecksum = 0;
                for (int i = 0, l = message.Length; i < l - 2; i++)
                {
                    checksum += message[i];
                }
                messageChecksum = message[message.Length - 2] << 8;
                messageChecksum += message[message.Length - 1];
                result = checksum == messageChecksum;
            }
            return result;
        }

		void DeviceFrameReady(object sender, IFrame frame)
		{
            BitmapSource source = frame.ToBitmapSource();
            videoImage.Source = source;
            if (captureFrame)
            {
                bitmapFrame = BitmapFrame.Create(source);
                OnBitmapFrameCaptured(bitmapFrame);
                captureFrame = false;
            }
            frame.Dispose();
            GC.Collect(1);
            Thread.Yield();
		}

		private void WindowClosed(object sender, EventArgs e) {
            Dashboard.Dispatcher.Invoke(
                DispatcherPriority.Normal,
                new Action( () => { Dashboard.Close(); } ));
		}
	}
}
