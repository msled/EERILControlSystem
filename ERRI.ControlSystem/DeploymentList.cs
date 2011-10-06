using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using EERIL.ControlSystem;

namespace EERIL.ControlSystem {
	class DeploymentList : ThreadObservableCollection<IDeployment>, IDeploymentList {
		private readonly Mission mission;

		internal DeploymentList(Mission mission) {
			this.mission = mission;
		}

		public IDeployment Create(DateTime dateTime, string notes, IList<IDevice> devices) {
			IDeployment deployment;
			DirectoryInfo deploymentsDirectory = new DirectoryInfo(Path.Combine(this.mission.Directory.FullName, dateTime.Ticks.ToString()));
			deploymentsDirectory.Create();
			deployment = Deployment.Create(dateTime, deploymentsDirectory, notes, devices);
			this.Add(deployment);
			return deployment;
		}

		public void RefreshList() {
			this.Clear();
			foreach (DirectoryInfo directory in mission.Directory.GetDirectories()) {
				this.Add(Deployment.Load(directory));
			}
		}
	}
}
