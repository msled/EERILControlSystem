using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EERIL.ControlSystem.Test;
using System.Threading;

namespace EERIL.ControlSystem.v4
{
    class PowerTest : ITest
    {
        private Timer timer;
        private IDevice device;

        public event OperationCompleteHandler OperationComplete;

        public event RestartOperationHandler RestartOperation;

        public event TestFailedHandler TestFailed;

        public event TestSuccessfulHandler TestSuccessful;

        public string Title
        {
            get { return "Power Test"; }
        }

        public string Instructions
        {
            get { return "The device will now freak out, hang out while it consumes some power."; }
        }

        public bool Active
        {
            get;
            set;
        }

        public PowerTest(IDevice device)
        {
            this.device = device;
        }

        public void OnOperationComplete(IOperation next)
        {
            if (OperationComplete != null)
            {
                OperationComplete(next);
            }
        }

        public void OnRestartOperation()
        {
            if (RestartOperation != null)
            {
                RestartOperation();
            }
        }

        public void OnTestFailed(String message)
        {
            if (TestFailed != null)
            {
                TestFailed(message);
            }
        }

        public void OnTestSuccessful()
        {
            if (TestSuccessful != null)
            {
                TestSuccessful();
            }
        }

        public IOperation Begin()
        {
            Random random = new Random();
            device.Open();
            timer = new Timer(delegate(Object state)
            {
                device.HorizontalFinPosition = Convert.ToByte(Math.Round(random.NextDouble() * 180));
                device.VerticalFinPosition = Convert.ToByte(Math.Round(random.NextDouble() * 180));
                device.Thrust = Convert.ToByte(Math.Round(random.NextDouble() * 180));
            }, device, 0, 1000);

            return null;
        }

        public void Cancel()
        {
            timer.Dispose();
        }
    }
}
