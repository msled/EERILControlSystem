using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;
using EERIL.ControlSystem.Communication;
using EERIL.ControlSystem.Properties;
using PvNET;

namespace EERIL.ControlSystem.Avt {
    internal class CommunicationsManager : ICommunicationsManager {
        private readonly ConcurrentQueue<ICommand> commandQueue = new ConcurrentQueue<ICommand>();
        private readonly ValuelessCommand heartbeat = new ValuelessCommand((byte)CommandCode.Heartbeat);
        private readonly Settings settings = Settings.Default;
        private readonly List<byte> serialBuffer = new List<byte>();
        private readonly CameraSerial serial;
        private readonly Timer heartbeatTimer;
        private readonly Thread serialMonitorThread;
        private readonly Thread transmissionThread;

        private uint? camera;

        public bool Connected { get; private set; }

        public event MessageHandler MessageReceived;

        internal CommunicationsManager(uint camera) {
            this.camera = camera;
            serial = new CameraSerial(camera);
            heartbeatTimer = new Timer(Heartbeat, null, 100, 100);
            serialMonitorThread = new Thread(MonitorSerialCommunication) {
                Name = "Serial Communication Monitor", 
                IsBackground = true, 
                Priority = ThreadPriority.BelowNormal
            };
            serialMonitorThread.Start();
            transmissionThread = new Thread(TransmitSerialCommand) {Name = "Serial Transmission Thread", IsBackground = true};
            transmissionThread.Start();
            Connected = true;
        }

        public void TransmitCommand(ICommand command) {
            commandQueue.Enqueue(command);
        }

        protected void OnMessageReceived(IMessage message) {
            if(MessageReceived == null) {
                return;
            }

            MessageHandler eventHandler = MessageReceived;
            Delegate[] delegates = eventHandler.GetInvocationList();
            foreach (MessageHandler handler in delegates) {
                DispatcherObject dispatcherObject = handler.Target as DispatcherObject;
                if (dispatcherObject != null && !dispatcherObject.CheckAccess()) {
                    dispatcherObject.Dispatcher.Invoke(DispatcherPriority.DataBind, handler, message);
                } else {
                    handler(message);
                }
            }
        }

        protected bool WriteBytesToSerial(byte[] buffer) {
            heartbeatTimer.Change(100, 100);
            return camera.HasValue && serial.WriteBytesToSerialIo(camera.Value, buffer, Convert.ToUInt32(buffer.LongLength));
        }

        protected bool ReadBytesFromSerial(byte[] buffer, ref uint recieved) {
            return camera.HasValue && serial.ReadBytesFromSerialIo(camera.Value, buffer, Convert.ToUInt32(buffer.LongLength), ref recieved);
        }

        internal bool QueueFrame(IntPtr framePointer, tFrameCallback callback) {
            if (!camera.HasValue) {
                throw new PvException(tErr.eErrUnavailable);
            }
            tErr error;
            lock (serial) {
                error = Pv.CaptureQueueFrame(camera.Value, framePointer, callback);
            }
            if (error != tErr.eErrSuccess) {
                return false;
            }
            return true;
        }

        public void Heartbeat(object state) {
            TransmitCommand(heartbeat);
        }

        private void TransmitSerialCommand() {
            ICommand command;
            tErr error;
            while (true) {
                if(commandQueue.TryDequeue(out command)) {
                    if( !WriteBytesToSerial(command.Command) ) {
                        Connected = false;
                    }
                }
                if (!Connected) {
                    lock (serial) {
                        do {
                            error = Pv.AttrExists(camera.Value, "WhiteBalance");
                            if (error != tErr.eErrUnavailable && error != tErr.eErrUnplugged && error != tErr.eErrTimeout) {
                                Connected = true;
                            }
                        } while (!Connected);
                    }
                }
                Thread.Yield();
            }
        }

        private void MonitorSerialCommunication() {
            byte[] buffer = new byte[settings.SerialReceiveInputBufferSize];
            uint length = 0;
            while (true) {
                lock (buffer) {
                    if (ReadBytesFromSerial(buffer, ref length) && length > 0) {
                        Connected = true;
                        ParseSerial(buffer, length);
                    }
                }
                Thread.Yield();
            }
        }

        private void ParseSerial(byte[] array, uint length) {
            for (int i = 0; i < length; i++) {
                if (array[i] != 0x0D) {
                    serialBuffer.Add(array[i]);
                } else if (serialBuffer.Count > 0) {
                    byte[] message = serialBuffer.ToArray();
                    serialBuffer.Clear();
                    OnMessageReceived(new InternalMessage(message));
                }
            }
        }

        public void Dispose() {
            serialMonitorThread.Abort();
            transmissionThread.Abort();
            heartbeatTimer.Dispose();
        }

        private class InternalMessage : IMessage {
            private readonly byte[] message;

            internal InternalMessage (byte[] message) {
                this.message = message;
                Type = (MessageType)message[0];
            }

            public MessageType Type { get; private set; }
            public byte[] Message { 
                get {
                    return message;
                } 
            }
        }
    }
}