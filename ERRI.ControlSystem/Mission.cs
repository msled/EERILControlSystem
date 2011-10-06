using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EERIL.ControlSystem {
	internal sealed class Mission : IMission {
		private IDeploymentList deployments;
		public string Name {
			get;
			private set;
		}
		public DirectoryInfo Directory {
			get;
			private set;
		}
		public IDeploymentList Deployments {
			get {
				if (deployments == null) {
					deployments = new DeploymentList(this);
				}
				return deployments;
			}
		}

		public Mission(string name, DirectoryInfo directory) {
			this.Name = name;
			this.Directory = directory;
		}
		public override string ToString() {
			return Name;
		}
	}
}
