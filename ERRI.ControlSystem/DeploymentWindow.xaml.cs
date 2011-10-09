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
using System.Windows.Threading;
using System.Threading;
using EERIL.ControlSystem.Properties;

namespace EERIL.ControlSystem {
	/// <summary>
	/// Interaction logic for Deployment.xaml
	/// </summary>
	public partial class DeploymentWindow : Window {
		private readonly App application = App.Current as App;
		private bool showOwner = true;

		public IList<IDevice> Devices {
			get;
			set;
		}

		private void addMissionButton_Click(object sender, RoutedEventArgs e) {
			application.Missions.Create(addMissionTextBox.Text);
		}

		public DeploymentWindow(IList<IDevice> devices) {
			Devices = devices;
			InitializeComponent();
			missionList.ItemsSource = application.Missions;
			dateTimeTextBox.Text = DateTime.Now.ToString();
		}

		private void deployButton_Click(object sender, RoutedEventArgs e) {
			IMission mission = missionList.SelectedItem as IMission;
			if(mission == null){
				MessageBox.Show("A mission must be selected for this deployment.");
				return;
			}
			Settings.Default.SelectedMission = mission.Name;
			Settings.Default.Save();

			IDeployment deployment = mission.Deployments.Create(DateTime.Parse(this.dateTimeTextBox.Text), new TextRange(this.notesRichTextBox.Document.ContentStart, this.notesRichTextBox.Document.ContentEnd).Text, Devices);


            Controller controller = new Controller(ControllerIndex.One);
            DashboardWindow dashboardWindow = new DashboardWindow(mission, deployment);
            VideoDisplayWindow videoDisplayWindow;
            Thread videoDisplayThread = new Thread(new ParameterizedThreadStart(delegate (Object args){
                Object[] argArray = args as Object[];
                videoDisplayWindow = new VideoDisplayWindow(argArray[0] as IMission, argArray[1] as IDeployment);
                videoDisplayWindow.Controller = controller;
                videoDisplayWindow.Dashboard = dashboardWindow;
                dashboardWindow.VideoDisplay = videoDisplayWindow;
			    videoDisplayWindow.Show();
                System.Windows.Threading.Dispatcher.Run();
            }));

            videoDisplayThread.SetApartmentState(ApartmentState.STA);
            videoDisplayThread.IsBackground = true;
            videoDisplayThread.Start(new Object[] { mission, deployment, this.Owner });
            dashboardWindow.Controller = controller;
			dashboardWindow.Show();

			showOwner = false;
			this.Close();
		}

		private void WindowClosed(object sender, EventArgs e) {
			if (showOwner) {
				Owner.Show();
			}
		}

		private void MissionListSelectionChanged(object sender, SelectionChangedEventArgs e) {
			deployButton.IsEnabled = missionList.SelectedItem != null;
		}

		private void CancelButtonClick(object sender, RoutedEventArgs e) {
			this.Close();
		}
	}
}
