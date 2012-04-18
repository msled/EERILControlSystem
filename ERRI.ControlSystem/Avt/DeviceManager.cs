using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using EERIL.ControlSystem.v4;
using System.Windows.Threading;
using System.Threading;
using System.Windows;
using EERIL.ControlSystem;
using System.ComponentModel;

namespace EERIL.ControlSystem.Avt
{
	class DeviceManager : IDeviceManager {
		private readonly GigEVision cameraManager;
		private IDevice activeDevice;
		private readonly ThreadObservableCollection<IDevice> devices = new ThreadObservableCollection<IDevice>();

		public IList<IDevice> Devices {
			get {
				return devices;
			}
		}

		public IDevice ActiveDevice {
			get {
				return activeDevice;
			}
			set {
				activeDevice = value;
				if (PropertyChanged != null) {
					PropertyChanged(this, new PropertyChangedEventArgs("ActiveDevice"));
				}
			}
		}

		void  CameraManagerCameraConnected(Camera camera) {
			devices.Add(new v4.Device(camera));
		}

		void CameraManagerCameraDisconnected(Camera camera) {
		    IDevice[] deviceArray = devices.Select(d => d.Id == camera.Reference ? d : null).Where(device => device != null).ToArray();
            foreach (IDevice device in deviceArray)
		    {
		        devices.Remove(device);
		    }
		}

	    public DeviceManager() {
			cameraManager = new Avt.GigEVision();

			cameraManager.CameraConnected +=CameraManagerCameraConnected;
			cameraManager.CameraDisconnected += CameraManagerCameraDisconnected;
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}
}