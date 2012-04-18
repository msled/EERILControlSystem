using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EERIL.ControlSystem
{
    class TerminateDeploymentException : Exception {
        public TerminateDeploymentException() : base(){}
        public TerminateDeploymentException(string message) : base(message) {}
    }
}
