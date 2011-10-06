using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using System.Collections.ObjectModel;

namespace EERIL.ControlSystem {
	public class ThreadObservableCollection<T> : ObservableCollection<T> {
		public override event System.Collections.Specialized.NotifyCollectionChangedEventHandler CollectionChanged;

		protected override void OnCollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
			using (BlockReentrancy()) {
				System.Collections.Specialized.NotifyCollectionChangedEventHandler eventHandler = CollectionChanged;
				if (eventHandler == null)
					return;

				Delegate[] delegates = eventHandler.GetInvocationList();
				foreach (System.Collections.Specialized.NotifyCollectionChangedEventHandler handler in delegates) {
					DispatcherObject dispatcherObject = handler.Target as DispatcherObject;
					if (dispatcherObject != null && !dispatcherObject.CheckAccess()) {
						dispatcherObject.Dispatcher.Invoke(DispatcherPriority.DataBind, handler, this, e);
					} else
						handler(this, e);
				}
			}
		}
	}
}
