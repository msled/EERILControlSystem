using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using EERIL.ControlSystem.Mock;

namespace EERIL.ControlSystem {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {

		public MainWindow() {
			InitializeComponent();

			deviceList.ItemsSource = (Application.Current as App).DeviceManager.Devices;
		}

		private void deployButton_Click(object sender, RoutedEventArgs e) {
			IList<IDevice> devices = new List<IDevice>(deviceList.SelectedItems.Count);
			foreach (IDevice device in deviceList.SelectedItems) {
				devices.Add(device);
			}

            if(devices.Count < 1)
            {
                devices.Add(new MockDevice());
            }

			DeploymentWindow deploymentWindow = new DeploymentWindow(devices);
			deploymentWindow.Owner = this;
			this.Hide();
			deploymentWindow.Show();
		}

		private void testButton_Click(object sender, RoutedEventArgs e) {
			TestWindow testWindow = new TestWindow(deviceList.SelectedItem as IDevice);
			testWindow.Owner = this;
			this.Hide();
			testWindow.Show();

		}

		/*void UniControl_OnNodeListChanged() {
			if (!cameraTree.Dispatcher.CheckAccess()) { // if not from this thread invoke it in our context
				cameraTree.Dispatcher.Invoke(DispatcherPriority.Normal, new UniControl.OnNodelistChangeHandler(UniControl_OnNodeListChanged));
				return;
			}
			treeLock.WaitOne();
			cameras = UniControl.GetCameras();
			TreeViewItem root = new TreeViewItem()
			{
				Header = "Devices"
			}; // root node for tree
			root.Tag = null;
			UniCamera camera;
			for (uint i = 0; i < cameras.Length; ++i)            // for every found camera
            {
				camera = cameras[0];
				try {
					camera.Open();                                 // open camera and attach to tree
					root.Items.Add(new TreeViewItem()
					{
						Header = String.Format("Device " + camera.SerialNumber),
						Tag = camera
					});                          // ready, so close the camera
					camera.Close();
				} catch (UniControlException ex) // handle camera open fail
				{
					MessageBox.Show(ex.ToString());
				}
			}
			cameraTree.BeginInit();                           // disable refresh
			cameraTree.Items.Clear();                           // clear tree
			cameraTree.Items.Add(root);
			cameraTree.EndInit();                                 // ready with the tree update, so refresh
			treeLock.ReleaseMutex();   
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
			UniControl.Release();
		}

		private void cameraTree_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			TreeViewItem item = cameraTree.SelectedItem as TreeViewItem;
			if (item != null && item.Tag != null) {
				UniCamera camera = item.Tag as UniCamera;
			}
		}*/
	}
}
