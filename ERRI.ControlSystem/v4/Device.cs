using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;
using EERIL.ControlSystem.Avt;
using EERIL.ControlSystem.Properties;
using EERIL.ControlSystem.Test;

namespace EERIL.ControlSystem.v4 {
    [ControlSystem.Device("MsledOS", OsVersion = new double[] {4})]
    internal class Device : IDevice {
        //private readonly ICamera camera;
        private readonly List<byte> buffer = new List<byte>();
        private readonly List<ICamera> cameras = new List<ICamera>();
        private readonly string displayName;
        private readonly Camera primaryCamera;
        private readonly List<ISensor> sensors = new List<ISensor>();
        private readonly Thread serialMonitorThread;
        private readonly Settings settings = Settings.Default;
        private readonly List<ITest> tests = new List<ITest>();
        private byte bottomFinOffset;
        private byte finRange;
        private byte focus;
        private byte horizontalFinPosition = 90;
        private byte illumination;
        private byte leftFinOffset;
        private PowerConfigurations powerConfiguration;
        private byte rightFinOffset;
        private byte thrust;
        private byte topFinOffset;
        private byte verticalFinPosition = 90;

        public Device(string displayName) {
            this.displayName = displayName;
        }

        public Device(Camera camera) {
            primaryCamera = camera;
            cameras.Add(camera);

            tests.Add(new PowerTest(this));
            serialMonitorThread = new Thread(MonitorSerialCommunication);
            serialMonitorThread.Name = "Serial Communication Monitor";
            serialMonitorThread.IsBackground = true;
            serialMonitorThread.Priority = ThreadPriority.BelowNormal;
        }

        #region IDevice Members

        public event DeviceMessageHandler MessageReceived;
        public uint Id {
            get {
                return primaryCamera.Reference;
            }
        }

        public string DisplayName {
            get {
                return displayName ?? primaryCamera.DisplayName;
            }
        }

        public IList<ITest> Tests {
            get {
                return tests.AsReadOnly();
            }
        }

        public IList<ISensor> Sensors {
            get {
                return sensors.AsReadOnly();
            }
        }

        public IList<ICamera> Cameras {
            get {
                return cameras.AsReadOnly();
            }
        }

        public ICamera PrimaryCamera {
            get {
                return primaryCamera;
            }
        }

        public byte HorizontalFinPosition {
            get {
                return horizontalFinPosition;
            }
            set {
                value = EnforceFinRange(value);
                if (!primaryCamera.WriteBytesToSerial(new byte[] {0x68, value, 0x0D})) {
                    throw new Exception("Failed to transmit horizontal fin position to device.");
                }
                horizontalFinPosition = value;
            }
        }

        public byte VerticalFinPosition {
            get {
                return verticalFinPosition;
            }
            set {
                value = EnforceFinRange(value);
                if (!primaryCamera.WriteBytesToSerial(new byte[] {0x76, value, 0x0D})) {
                    throw new Exception("Failed to transmit vertical fin position to device.");
                }
                verticalFinPosition = value;
            }
        }

        public byte TopFinOffset {
            get {
                return topFinOffset;
            }
            set {
                if (!primaryCamera.WriteBytesToSerial(new byte[] {0x61, 0x74, value, 0x0D})) {
                    throw new Exception("Failed to transmit top fin offset.");
                }
                topFinOffset = value;
            }
        }

        public byte RightFinOffset {
            get {
                return rightFinOffset;
            }
            set {
                if (!primaryCamera.WriteBytesToSerial(new byte[] {0x61, 0x72, value, 0x0D})) {
                    throw new Exception("Failed to transmit right fin offset.");
                }
                rightFinOffset = value;
            }
        }

        public byte BottomFinOffset {
            get {
                return bottomFinOffset;
            }
            set {
                if (!primaryCamera.WriteBytesToSerial(new byte[] {0x61, 0x62, value, 0x0D})) {
                    throw new Exception("Failed to transmit bottom fin offset.");
                }
                bottomFinOffset = value;
            }
        }

        public byte LeftFinOffset {
            get {
                return leftFinOffset;
            }
            set {
                if (!primaryCamera.WriteBytesToSerial(new byte[] {0x61, 0x6C, value, 0x0D})) {
                    throw new Exception("Failed to transmit left fin offset.");
                }
                leftFinOffset = value;
            }
        }

        public byte FinRange {
            get {
                return finRange;
            }
            set {
                finRange = value;
                HorizontalFinPosition = horizontalFinPosition;
                VerticalFinPosition = verticalFinPosition;
            }
        }

        public bool Turbo { get; set; }

        public byte Thrust {
            get {
                return thrust;
            }
            set {
                if (!primaryCamera.WriteBytesToSerial(new byte[] {0x74, Convert.ToByte(Turbo ? value : (value - 90)/2 + 90), 0x0D})) {
                    throw new Exception("Failed to transmit thrust to device.");
                }
                thrust = value;
            }
        }

        public byte Illumination {
            get {
                return illumination;
            }
            set {
                if (!primaryCamera.WriteBytesToSerial(new byte[] {0x69, value, 0x0D})) {
                    throw new Exception("Failed to transmit illumination.");
                }
                illumination = value;
            }
        }

        public byte Focus
        {
            get { return focus; }
            set
            {
                if (!primaryCamera.WriteBytesToSerial(new byte[] { 0x66, value, 0x0D }))
                {
                    throw new Exception("Failed to transmit focus position to device.");
                }
                focus = value;
            }
        }

        public PowerConfigurations PowerConfiguration {
            get {
                return powerConfiguration;
            }
            set {
                if (!primaryCamera.WriteBytesToSerial(new byte[] {0x70, (byte) value, 0x0D})) {
                    throw new Exception("Failed to transmit power configuration.");
                }
                powerConfiguration = value;
            }
        }

        public void Open() {
            serialMonitorThread.Start();
        }

        public void Close() {
            serialMonitorThread.Abort();
        }

        public void Dispose() {
            foreach (ICamera camera in cameras) {
                camera.Close();
            }
        }

        public void CalibrateIMU()
        {
            if (!primaryCamera.WriteBytesToSerial(new byte[] { 0x63, 0x0D }))
            {
                throw new Exception("Failed to configure IMU.");
            }
        }

        #endregion

        private byte EnforceFinRange(byte value) {
            int adjustedValue = value - 90, invertedRange;
            byte result;
            if (adjustedValue > FinRange) {
                result = (byte) (FinRange + 90);
            } else if (adjustedValue < (invertedRange = FinRange*-1)) {
                result = (byte) (invertedRange + 90);
            } else {
                result = value;
            }
            return result;
        }

        protected void OnMessageReceived(byte[] message) {
            if (MessageReceived != null) {
                DeviceMessageHandler eventHandler = MessageReceived;
                Delegate[] delegates = eventHandler.GetInvocationList();
                foreach (DeviceMessageHandler handler in delegates) {
                    var dispatcherObject = handler.Target as DispatcherObject;
                    if (dispatcherObject != null && !dispatcherObject.CheckAccess()) {
                        dispatcherObject.Dispatcher.Invoke(DispatcherPriority.DataBind, handler, this, message);
                    } else {
                        handler(this, message);
                    }
                }
            }
        }

        private void MonitorSerialCommunication() {
            var buffer = new byte[settings.SerialReceiveInputBufferSize];
            uint length = 0;
            while (true) {
                lock (buffer) {
                    if (primaryCamera.ReadBytesFromSerial(buffer, ref length) && length > 0) {
                        ParseSerial(buffer, length);
                    }
                }
                Thread.Yield();
            }
        }

        private void ParseSerial(byte[] array, uint length) {
            for (int i = 0; i < length; i++) {
                if (array[i] != 0x0D) {
                    buffer.Add(array[i]);
                } else if (buffer.Count > 0) {
                    byte[] message = buffer.ToArray();
                    buffer.Clear();
                    OnMessageReceived(message);
                }
            }
        }
    }
}