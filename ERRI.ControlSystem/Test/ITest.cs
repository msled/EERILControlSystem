using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EERIL.ControlSystem.Test {
	public delegate void OperationCompleteHandler(IOperation next);

	public delegate void RestartOperationHandler();

	public delegate void TestFailedHandler(string message);

	public delegate void TestSuccessfulHandler();

	public interface ITest {
		event OperationCompleteHandler OperationComplete;
		event RestartOperationHandler RestartOperation;
		event TestFailedHandler TestFailed;
		event TestSuccessfulHandler TestSuccessful;

		string Title { get; }
		string Instructions { get; }
		Boolean Active { get; set; }

		IOperation Begin();
        void Cancel();
	}
}
