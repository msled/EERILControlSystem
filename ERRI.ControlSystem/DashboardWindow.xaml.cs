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
using Microsoft.Xna.Framework.Input;
using EERIL.ControlSystem;
using System.Windows.Threading;
using System.IO;

namespace EERIL.ControlSystem {
	/// <summary>
	/// Interaction logic for Dashboard.xaml
	/// </summary>
	public partial class DashboardWindow : Window {
		private Controller controller = null;
		private IDeviceManager deviceManager;
        private VideoDisplayWindow videoDisplayWindow = null;
		private readonly ControllerAxisChangedHandler controllerAxisChangedHandler;
		private readonly ControllerConnectionChangedHandler controllerConnectionChangedHandler;
        private readonly BitmapFrameCapturedHandler bitmapFrameCapturedHandler;
		public Controller Controller {
			get {
				return controller;
			}
			set {
				if (controller != null) {
					controller.AxisChanged -= controllerAxisChangedHandler;
					controller.ConnectionChanged -= controllerConnectionChangedHandler;
				}
                controller = value;
                if (controller != null)
                {
                    controller.AxisChanged += controllerAxisChangedHandler;
                    controller.ConnectionChanged += controllerConnectionChangedHandler;
                }
			}
		}
		public VideoDisplayWindow VideoDisplay {
            get
            {
                return videoDisplayWindow;
            }
            set
            {
                if(videoDisplayWindow != null){
                    videoDisplayWindow.BitmapFrameCaptured -= bitmapFrameCapturedHandler;
                }
                videoDisplayWindow = value;
                if(videoDisplayWindow != null){
                    videoDisplayWindow.BitmapFrameCaptured += bitmapFrameCapturedHandler;
                }
            }
		}

        void VideoDisplayWindowBitmapFrameCaptured(BitmapFrame frame)
        {
            throw new NotImplementedException();
        }

		private IDeployment Deployment {
			get;
			set;
		}

		private IMission Mission {
			get;
			set;
		}

		public DashboardWindow(IMission mission, IDeployment deployment) {
			InitializeComponent();
			Mission = mission;
			Deployment = deployment;
			deviceManager = (Application.Current as App).DeviceManager;
			if (deployment.Devices.Count > 0) {
				deviceManager.ActiveDevice = deployment.Devices[0];
				deviceManager.ActiveDevice.MessageReceived += new DeviceMessageHandler(ActiveDeviceMessageReceived);
			}
            imuButton.DataContext = false;
			controllerAxisChangedHandler = new ControllerAxisChangedHandler(ControllerAxisChanged);
			controllerConnectionChangedHandler = new ControllerConnectionChangedHandler(ControllerConnectionChanged);
            bitmapFrameCapturedHandler = new BitmapFrameCapturedHandler(VideoDisplayWindowBitmapFrameCaptured);
			this.Title = String.Format("Dashboard - {0} > {1}", mission.Name, deployment.DateTime.ToString());
            TopFinOffsetSlider.ValueChanged += new RoutedPropertyChangedEventHandler<double>(TopFinOffsetSlider_ValueChanged);
            RightFinOffsetSlider.ValueChanged += new RoutedPropertyChangedEventHandler<double>(RightFinOffsetSlider_ValueChanged);
            BottomFinOffsetSlider.ValueChanged += new RoutedPropertyChangedEventHandler<double>(BottomFinOffsetSlider_ValueChanged);
            LeftFinOffsetSlider.ValueChanged += new RoutedPropertyChangedEventHandler<double>(LeftFinOffsetSlider_ValueChanged);
			/*dashboardChart.DataContext = new KeyValuePair<DateTime, int>[] { 
				new KeyValuePair<DateTime, int>(new DateTime(2011, 12, 25, 18, 30, 24, DateTimeKind.Utc), 12), 
				new KeyValuePair<DateTime, int>(new DateTime(2011, 12, 25, 18, 30, 34, DateTimeKind.Utc), 23), 
				new KeyValuePair<DateTime, int>(new DateTime(2011, 12, 25, 18, 30, 44, DateTimeKind.Utc), 12), 
				new KeyValuePair<DateTime, int>(new DateTime(2011, 12, 25, 18, 30, 54, DateTimeKind.Utc), 32), 
				new KeyValuePair<DateTime, int>(new DateTime(2011, 12, 25, 18, 31, 4, DateTimeKind.Utc), 15)
			};*/
		}

        void LeftFinOffsetSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            deviceManager.ActiveDevice.LeftFinOffset = Convert.ToByte(e.NewValue);
        }

        void BottomFinOffsetSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            deviceManager.ActiveDevice.BottomFinOffset = Convert.ToByte(e.NewValue);
        }

        void RightFinOffsetSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            deviceManager.ActiveDevice.RightFinOffset = Convert.ToByte(e.NewValue);
        }

        void TopFinOffsetSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            deviceManager.ActiveDevice.TopFinOffset = Convert.ToByte(e.NewValue);
        }

        void ActiveDeviceMessageReceived(object sender, byte[] message)
        {
			//serialData.Text += message;
		}

		void ControllerConnectionChanged(bool connected) {
			state.Content = connected ? "Connected" : "Disconnected";
		}

		void ControllerAxisChanged(ControllerJoystick joystick, ControllerJoystickAxis axis, byte oldValue, byte newValue) {
			IDevice device = deviceManager.ActiveDevice;
			if (device != null) {
				switch (joystick) {
					case ControllerJoystick.Left:
						switch (axis) {
							case ControllerJoystickAxis.X:
                                try
                                {
                                    device.HorizontalFinPosition = newValue;
                                }
                                catch (Exception ex)
                                {
                                    serialData.Text += ex.Message + '\n';
                                }
								break;
							case ControllerJoystickAxis.Y:
                                try
                                {
                                    device.VerticalFinPosition = newValue;
                                }
                                catch (Exception ex)
                                {
                                    serialData.Text += ex.Message + '\n';
                                }
								break;
						}
                        break;
                    case ControllerJoystick.Right:
                        switch (axis)
                        {
                            case ControllerJoystickAxis.Y:
                                try
                                {
                                    device.Thrust = newValue;
                                }
                                catch (Exception ex)
                                {
                                    serialData.Text += ex.Message + '\n';
                                }
                                break;
                        }
                        break;
				}
			}
		}

		private void Window_Closed(object sender, EventArgs e) {
            if (VideoDisplay.IsVisible)
            {
                VideoDisplay.Dispatcher.Invoke(
                    DispatcherPriority.Normal,
                    new Action(() => { VideoDisplay.Close(); }));
            }
            deviceManager.ActiveDevice.Close();
            (Application.Current as App).MainWindow.Show();
		}

        private void imuButton_Click(object sender, RoutedEventArgs e)
        {
            IDevice device = deviceManager.ActiveDevice;
            device.IsImuActive = !device.IsImuActive;
            imuButton.Content = "IMU " + (device.IsImuActive ? "On" : "Off");
            //imuButton.IsEnabled = false;
        }
	}
}
