using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.Specialized;

namespace EERIL.ControlSystem {
	public interface IMissionList : ICollection<IMission>,
	INotifyCollectionChanged, INotifyPropertyChanged {
		IMission Create(string name);
		void RefreshList();
	}
}
