using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.Specialized;

namespace EERIL.ControlSystem {
	public delegate void DeviceListChangedHandler(object sender, EventArgs e);

	public interface IDeviceManager : INotifyPropertyChanged {
		IList<IDevice> Devices { get; }
		IDevice ActiveDevice { get; set; }
	}
}
