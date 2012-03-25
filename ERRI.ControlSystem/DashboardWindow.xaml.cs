using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace EERIL.ControlSystem {
	/// <summary>
	/// Interaction logic for Dashboard.xaml
	/// </summary>
	public partial class DashboardWindow : Window {
		private Controller controller = null;
		private IDeviceManager deviceManager;
		private VideoDisplayWindow videoDisplayWindow = null;
		private readonly Properties.Settings settings = Properties.Settings.Default;
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
			return;
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
				deviceManager.ActiveDevice.MessageReceived += ActiveDeviceMessageReceived;
			}
			controllerAxisChangedHandler = ControllerAxisChanged;
			controllerConnectionChangedHandler = ControllerConnectionChanged;
			bitmapFrameCapturedHandler = VideoDisplayWindowBitmapFrameCaptured;
			this.Title = String.Format("Dashboard - {0} > {1}", mission.Name, deployment.DateTime.ToString());
			YawOffsetSlider.ValueChanged += YawOffsetSliderValueChanged;
			FinRangeSlider.ValueChanged += FinRangeSliderValueChanged;
			TopFinOffsetSlider.ValueChanged += TopFinOffsetSliderValueChanged;
			RightFinOffsetSlider.ValueChanged += RightFinOffsetSliderValueChanged;
			BottomFinOffsetSlider.ValueChanged += BottomFinOffsetSliderValueChanged;
			LeftFinOffsetSlider.ValueChanged += LeftFinOffsetSliderValueChanged;
			illuminationSlider.ValueChanged += IlluminationSliderValueChanged;
		}

		void IlluminationSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			deviceManager.ActiveDevice.Illumination = Convert.ToByte(e.NewValue);
		}

		void YawOffsetSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			videoDisplayWindow.YawOffset = e.NewValue;
		}

		void FinRangeSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			deviceManager.ActiveDevice.FinRange = Convert.ToByte(e.NewValue);
		}

		void LeftFinOffsetSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			deviceManager.ActiveDevice.LeftFinOffset = Convert.ToByte(e.NewValue);
		}

		void BottomFinOffsetSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			deviceManager.ActiveDevice.BottomFinOffset = Convert.ToByte(e.NewValue);
		}

		void RightFinOffsetSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			deviceManager.ActiveDevice.RightFinOffset = Convert.ToByte(e.NewValue);
		}

		void TopFinOffsetSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
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

		private void WindowClosed(object sender, EventArgs e) {
			if (VideoDisplay.IsVisible)
			{
				VideoDisplay.Dispatcher.Invoke(
					DispatcherPriority.Normal,
					new Action(() => VideoDisplay.Close()));
			}
			deviceManager.ActiveDevice.Close();
			(Application.Current as App).MainWindow.Show();
		}

		private void RecordVideoButtonClick(object sender, RoutedEventArgs e)
		{
			VideoDisplay.RecordVideoStream = !VideoDisplay.RecordVideoStream;
			recordVideoButton.Content = VideoDisplay.RecordVideoStream ? "Video is Recording" : "Record Video";
		}

		private void PowerComboBoxSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			Control selected = (e.AddedItems[0] as Control);
			if (deviceManager != null && selected != null)
			{
				deviceManager.ActiveDevice.PowerConfiguration = (PowerConfigurations) Byte.Parse(selected.Tag as String);
			}
		}
	}
}
