using System;
using System.Collections.Generic;
using System.Windows.Threading;
using PvNET;

namespace EERIL.ControlSystem.Avt {
	public delegate void CameraConnectionHandler(ICamera camera);

	public class GigEVision {
		private readonly List<Camera> cameras = new List<Camera>();
	    private readonly tLinkCallback connectionChangedCallback;
		public event CameraConnectionHandler CameraConnected;
		public event CameraConnectionHandler CameraDisconnected;

		protected void OnCameraConnectionChanged(IntPtr context, tInterface iface, tLinkEvent evt, UInt32 uniqueId) {
			bool connected = evt == tLinkEvent.eLinkAdd;
			Camera camera;
			if (connected) {
				tCameraInfo cameraInfo = new tCameraInfo();
				tErr err = Pv.CameraInfo(uniqueId, ref cameraInfo);
				if (err != tErr.eErrSuccess) {
					throw new PvException(err);
				}
			    camera = new Camera(cameraInfo);
                cameras.Add(camera);
			} else {
				camera = cameras.Find(c => c.UniqueId == uniqueId);
			}
			if (camera != null && (CameraConnected != null && connected) || (CameraDisconnected != null && !connected)) {
				CameraConnectionHandler eventHandler = connected ? CameraConnected : CameraDisconnected;
				Delegate[] delegates = eventHandler.GetInvocationList();
				foreach (CameraConnectionHandler handler in delegates) {
					DispatcherObject dispatcherObject = handler.Target as DispatcherObject;
					if (dispatcherObject != null && !dispatcherObject.CheckAccess()) {
						dispatcherObject.Dispatcher.Invoke(DispatcherPriority.DataBind, handler, camera);
					} else
						handler(camera);
				}
			}
		}

		public GigEVision() {
            tErr error = Pv.Initialize();
            this.connectionChangedCallback = OnCameraConnectionChanged;
            if (error == tErr.eErrSuccess)
            {
                error = Pv.LinkCallbackRegister(this.connectionChangedCallback, tLinkEvent.eLinkAdd, IntPtr.Zero);
                if (error == tErr.eErrSuccess)
                {
                    error = Pv.LinkCallbackRegister(this.connectionChangedCallback, tLinkEvent.eLinkRemove, IntPtr.Zero);
                }
            }
            if (error != tErr.eErrSuccess)
            {
                throw new PvException(error);
            }
		}

		~GigEVision() {
            Pv.UnInitialize();
		}
	}
}
