using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EERIL.ControlSystem {
	public interface IDeployment {
		DateTime DateTime { get; }
		string Notes { get; set; }
		IList<IDevice> Devices { get; }
	}
}
