using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EERIL.ControlSystem.Test {
	public interface IOperation {
		string Title { get; }
		string Instructions { get; }
	}
}
