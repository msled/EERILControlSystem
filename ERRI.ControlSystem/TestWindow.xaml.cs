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
using EERIL.ControlSystem.Test;

namespace EERIL.ControlSystem {
	/// <summary>
	/// Interaction logic for TestWindow.xaml
	/// </summary>
	public partial class TestWindow : Window {
		private ITest curTest;

		public IDevice Device {
			get;
			set;
		}
		public TestWindow(IDevice device) {
			Device = device;
			InitializeComponent();
			if (device != null && device.Tests != null) {
				testTreeView.ItemsSource = device.Tests;
			}
		}

		private void Window_Closed(object sender, EventArgs e) {
			Owner.Show();
		}

		private void testTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
			beginButton.IsEnabled = testTreeView.SelectedItem != null;
		}

		private void cancelButton_Click(object sender, RoutedEventArgs e) {
			curTest.Cancel();
			this.Close();
		}

		private void beginButton_Click(object sender, RoutedEventArgs e)
		{
			curTest = testTreeView.SelectedItem as ITest;
			curTest.Begin();
		}
	}
}
