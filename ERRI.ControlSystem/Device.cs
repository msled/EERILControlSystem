using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EERIL.ControlSystem {
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	sealed class Device : Attribute {
		public double[] OsVersion;
		public string OsIdentifier;
		public CommunicationProtocols CommunicationProtocol;
		public Device(string osIdentifier) {
			OsIdentifier = osIdentifier;
			OsVersion = new double[]{-1};
			CommunicationProtocol = CommunicationProtocols.AvtGigE;
		}
	}
}
