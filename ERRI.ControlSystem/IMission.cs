using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EERIL.ControlSystem {
	public interface IMission {
		string Name { get; }
		IDeploymentList Deployments { get; }
	}
}
