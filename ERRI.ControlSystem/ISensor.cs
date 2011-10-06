using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EERIL.ControlSystem {
	public interface ISensor {
		string DisplayName { get; }
		string Description { get; }
		Stream Stream { get; }
	}
}
