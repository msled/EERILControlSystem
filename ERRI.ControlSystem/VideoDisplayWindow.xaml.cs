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
	/// <summary>
	/// Interaction logic for VideoDisplay.xaml
	/// </summary>
	public partial class VideoDisplayWindow
	{
		//private Bitmap videoCanvas;
		//private BitmapSizeOptions sizeOption;
		private readonly IDeviceManager deviceManager;
	    private const byte axisAngleDivisor = byte.MaxValue/180;
		//private Int32Rect rectangle;
		//private long lastDraw = 0L;
		private Controller controller;
        [return: MarshalAs(UnmanagedType.U1)]
		[DllImport("gdi32.dll")]
		protected static extern bool DeleteObject(IntPtr hObject);
        private List<byte> sent = new List<byte>(100);
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

		public VideoDisplayWindow(IMission mission, IDeployment deployment) {
			InitializeComponent();
			Mission = mission;
			Deployment = deployment;
			Title = String.Format("Video - {0} > {1}", mission.Name, deployment.DateTime.ToString());
			canvas.SizeChanged += CanvasSizeChanged;
		    var app = Application.Current as App;
		    if (app == null)
		    {
		       throw new Exception("Something has gone arye!"); 
		    }
            deviceManager = app.DeviceManager;
		    this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
		                                                                                                             {
				//headsUpDisplay.Width = (canvas.ActualWidth / 100) * Settings.Default.HeadsUpDisplayZoom;
				//Canvas.SetTop(headsUpDisplay, Settings.Default.HeadsUpDisplayLocation.Y);
				//Canvas.SetLeft(headsUpDisplay, Settings.Default.HeadsUpDisplayLocation.X);

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

		void ControllerButtonStateChanged(Button button) {
			if (button == Button.Y && !controller.Y) {
				//headsUpDisplay.Visibility = headsUpDisplay.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
			}
		}

		void CanvasSizeChanged(object sender, SizeChangedEventArgs e) {
			//headsUpDisplay.Width = (canvas.ActualWidth / 100) * Settings.Default.HeadsUpDisplayZoom;
		}

		void DeviceFrameReady(object sender, IFrame frame)
		{
            videoImage.Source = frame.ToBitmapSource();
            frame.Dispose();
            GC.Collect(1);
            Thread.Yield();
		}

		private void WindowClosed(object sender, EventArgs e) {
			Dashboard.Close();
		}
	}
}
