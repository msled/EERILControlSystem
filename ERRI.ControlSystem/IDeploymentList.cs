using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.ComponentModel;

namespace EERIL.ControlSystem {
	public interface IDeploymentList {
		IDeployment Create(DateTime dateTime, string notes, IList<IDevice> devices);
		void RefreshList();
	}
}
