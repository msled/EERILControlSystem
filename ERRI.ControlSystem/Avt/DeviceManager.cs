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
using PvNET;

namespace EERIL.ControlSystem.Avt
{
	class DeviceManager : IDeviceManager {
		private GigEVision cameraManager;
        
		private IDevice activeDevice = null;
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

		void  CameraManagerCameraConnected(ICamera camera) {
			devices.Add(new v4.Device(camera));
		}

		void CameraManagerCameraDisconnected(ICamera camera)
		{
		    foreach (IDevice device in devices.Select(d => d.Id == camera.Reference ? d : null).Where(device => device != null))
		    {
                devices.Remove(device);
            }
		}

	    public DeviceManager() {
			cameraManager = new Avt.GigEVision();

			cameraManager.CameraConnected +=new CameraConnectionHandler(CameraManagerCameraConnected);
			cameraManager.CameraDisconnected += new CameraConnectionHandler(CameraManagerCameraDisconnected);
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}
}