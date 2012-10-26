using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using EERIL.ControlSystem.Avt;

namespace EERIL.ControlSystem {
	public interface IDeployment {
		DateTime DateTime { get; }
		string Notes { get; set; }
		IList<IDevice> Devices { get; }
		void RecordFrame(IDevice device, IFrame frame);
		void Dispose();
        string LogCreate();
        void Bmpsave(Bitmap bitmap);
	}
}
