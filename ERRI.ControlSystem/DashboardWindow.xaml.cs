using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace EERIL.ControlSystem
{
    internal enum MessageLevel
    {
        Error,
        Normal,
        Info
    }
	/// <summary>
	/// Interaction logic for Dashboard.xaml
	/// </summary>
	public partial class DashboardWindow : Window {
		private Controller controller;
		private readonly IDeviceManager deviceManager;
	    private IMission mission;
	    private IDeployment deployment;
		private VideoDisplayWindow videoDisplayWindow;
		private readonly Properties.Settings settings = Properties.Settings.Default;
		private readonly ControllerAxisChangedHandler controllerAxisChangedHandler;
		private readonly ControllerConnectionChangedHandler controllerConnectionChangedHandler;
		private readonly BitmapFrameCapturedHandler bitmapFrameCapturedHandler;
	    private double serialScrollViewerHeight = 0;

	    public static DependencyProperty DeploymentProperty = DependencyProperty.Register("Deployment",
	                                                                                      typeof (IDeployment),
	                                                                                      typeof (DashboardWindow));

	    public static readonly DependencyProperty MissionProperty =
	        DependencyProperty.Register("Mission", typeof (IMission), typeof (DashboardWindow), new PropertyMetadata(default(IMission)));

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
		}

		public IDeployment Deployment {
            get { return GetValue(DeploymentProperty) as IDeployment; }
			private set { SetValue(DeploymentProperty, value);}
		}

        public IMission Mission
        {
            get { return GetValue(MissionProperty) as IMission; }
            set { SetValue(MissionProperty, value); }
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
            this.Title = String.Format("Dashboard - {0} > {1}", mission.Name, deployment.DateTime.ToString(CultureInfo.InvariantCulture));
            YawOffsetSlider.ValueChanged += YawOffsetSliderValueChanged;
            PitchOffsetSlider.ValueChanged += PitchOffsetSliderValueChanged;
			FinRangeSlider.ValueChanged += FinRangeSliderValueChanged;
			TopFinOffsetSlider.ValueChanged += TopFinOffsetSliderValueChanged;
			RightFinOffsetSlider.ValueChanged += RightFinOffsetSliderValueChanged;
			BottomFinOffsetSlider.ValueChanged += BottomFinOffsetSliderValueChanged;
			LeftFinOffsetSlider.ValueChanged += LeftFinOffsetSliderValueChanged;
			illuminationSlider.ValueChanged += IlluminationSliderValueChanged;
		}

        private void WriteToConsole(string line, MessageLevel level = MessageLevel.Normal)
        {
            /*Inline inline;
            if (level == MessageLevel.Normal)
            {
                inline = new Run(line);
            }
            else
            {
                inline = new Bold(new Span(new Run(line)) { Foreground = level == MessageLevel.Error ? Brushes.DarkRed : Brushes.DarkGreen });
            }
            serialTextBlock.ContentStart.InsertLineBreak();
            serialTextBlock.Inlines.InsertBefore(serialTextBlock.Inlines.FirstInline, inline);
            while (serialTextBlock.Inlines.Count > settings.MessageHistoryLength)
            {
                serialTextBlock.Inlines.Remove(serialTextBlock.Inlines.LastInline);
                serialTextBlock.Inlines.Remove(serialTextBlock.Inlines.LastInline);
            }*/
        }

		void IlluminationSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			deviceManager.ActiveDevice.Illumination = Convert.ToByte(e.NewValue);
		}

        void YawOffsetSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            videoDisplayWindow.YawOffset = e.NewValue;
        }

        void PitchOffsetSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            videoDisplayWindow.PitchOffset = e.NewValue;
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

		void ActiveDeviceMessageReceived(object sender, byte[] message) {
		    WriteToConsole('>' + BitConverter.ToString(message));
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
                                    //TODO: Route to console
									//serial.Text += ex.Message + '\n';
								}
								break;
							case ControllerJoystickAxis.Y:
								try
								{
									device.VerticalFinPosition = newValue;
								}
								catch (Exception ex)
                                {
                                    //TODO: Route to console
									//serialData.Text += ex.Message + '\n';
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
                                    //TODO: Route to console
									//serialData.Text += ex.Message + '\n';
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
				VideoDisplay.Dispatcher.BeginInvoke(
					DispatcherPriority.Send,
					new Action(() => VideoDisplay.Close()));
			}
			deviceManager.ActiveDevice.Close();
			(Application.Current as App).MainWindow.Show();
		}

		private void PowerComboBoxSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			Control selected = (e.AddedItems[0] as Control);
			if (deviceManager != null && selected != null)
			{
				deviceManager.ActiveDevice.PowerConfiguration = (PowerConfigurations) Byte.Parse(selected.Tag as String);
			}
		}

        private void DevicesTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
            devicePropertyGrid.Visibility = e.NewValue != null ? Visibility.Visible : Visibility.Hidden;
        }

        private void RecordVideoToggleButtonChecked(object sender, RoutedEventArgs e)
        {
            VideoDisplay.RecordVideoStream = true;
            recordVideoToggleButton.Content = "Video is Recording";
        }

        private void RecordVideoToggleButtonUnchecked(object sender, RoutedEventArgs e)
        {
            VideoDisplay.RecordVideoStream = false;
            recordVideoToggleButton.Content = "Record Video";
        }

        private void GridSplitterMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            if (serialScrollViewer.Visibility == Visibility.Collapsed) {
                serialScrollViewer.Visibility = Visibility.Visible;
                serialScrollViewer.Height = serialScrollViewerHeight;
                serialRow.Height = GridLength.Auto;
            }
            else
            {
                serialScrollViewerHeight = serialRow.ActualHeight;
                serialScrollViewer.Visibility = Visibility.Collapsed;
                serialRow.Height = GridLength.Auto;
            }
        }

        private void GridSplitterDragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            if (serialScrollViewer.Visibility == Visibility.Collapsed)
            {
                serialScrollViewer.Visibility = Visibility.Visible;
            }
        }

        private void CalibrateImuButtonClick(object sender, RoutedEventArgs e) {
            IDevice device = deviceManager.ActiveDevice;
            if (device != null)
            {
                device.CalibrateIMU();
            }
        }
	}
}
