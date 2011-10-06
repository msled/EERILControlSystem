using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PvNET;

namespace EERIL.ControlSystem.Avt {
	public class PvException : Exception {
		public string Message {
			get;
			private set;
		}
		internal PvException(tErr error) {
			switch (error) {
				case tErr.eErrCameraFault:
					this.Message = "Unexpected camera fault.";
					break;
				case tErr.eErrInternalFault:
					this.Message = "Unexpected fault in PvApi or driver.";
					break;
				case tErr.eErrBadHandle:
					this.Message = "Camera handle is invalid";
					break;
				case tErr.eErrBadParameter:
					this.Message = "Bad parameter to API call";
					break;
				case tErr.eErrBadSequence:
					this.Message = "Sequence of API calls is incorrect";
					break;
				case tErr.eErrNotFound:
					this.Message = "Camera or attribute not found";
					break;
				case tErr.eErrAccessDenied:
					this.Message = "Camera cannot be opened in the specified mode";
					break;
				case tErr.eErrUnplugged:
					this.Message = "Camera was unplugged";
					break;
				case tErr.eErrInvalidSetup:
					this.Message = "Setup is invalid (an attribute is invalid)";
					break;
				case tErr.eErrResources:
					this.Message = "System/network resources or memory not available";
					break;
				case tErr.eErrBandwidth:
					this.Message = "1394 bandwidth not available";
					break;
				case tErr.eErrQueueFull:
					this.Message = "Too many frames on queue";
					break;
				case tErr.eErrBufferTooSmall:
					this.Message = "Frame buffer is too small";
					break;
				case tErr.eErrCancelled:
					this.Message = "Frame cancelled by user";
					break;
				case tErr.eErrDataLost:
					this.Message = "The data for the frame was lost";
					break;
				case tErr.eErrDataMissing:
					this.Message = "Some data in the frame is missing";
					break;
				case tErr.eErrTimeout:
					this.Message = "Timeout during wait";
					break;
				case tErr.eErrOutOfRange:
					this.Message = "Attribute value is out of the expected range";
					break;
				case tErr.eErrWrongType:
					this.Message = "Attribute is not this type (wrong access function) ";
					break;
				case tErr.eErrForbidden:
					this.Message = "Attribute write forbidden at this time";
					break;
				case tErr.eErrUnavailable:
					this.Message = "Attribute is not available at this time.";
					break;
				default:
					this.Message = "An unknown exception has occured.";
					break;
			}
		}
	}
}
