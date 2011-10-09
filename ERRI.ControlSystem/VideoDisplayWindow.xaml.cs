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
            deviceManager = app.DeviceManager;
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
                case Trigger.Right:
                    if (pressed)
                    {
                        captureFrame = true;
                    }
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
