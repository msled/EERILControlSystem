using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.Xml;
using EERIL.ControlSystem.Avt;
using AForge.Video.VFW;

namespace EERIL.ControlSystem
{
    public class Deployment : IDeployment, IDisposable
    {
        private const string IMAGES_DIRECTORY = "Images";
        private const string SERIAL_DATA_DIRECTORY = "SerialData";
        private const string VIDEOS_DIRECTORY = "Videos";

        private DirectoryInfo videoDirectory;
        private readonly IDictionary<IDevice, AVIWriter> videoWriters = new ConcurrentDictionary<IDevice, AVIWriter>();
        private DirectoryInfo imageDirectory;
        private DirectoryInfo serialDataDirectory;
        private readonly IDictionary<IDevice, Stream> serialLogStreams = new ConcurrentDictionary<IDevice, Stream>();

        public DateTime DateTime
        {
            get;
            private set;
        }

        public DirectoryInfo Directory
        {
            get;
            private set;
        }


        public string Notes
        {
            get;
            set;
        }

        public IList<IDevice> Devices { get; private set; }

        private Deployment() { }

        public static IDeployment Create(DateTime dateTime, DirectoryInfo directory, string notes, IList<IDevice> devices)
        {
            Deployment deployment = new Deployment
                                        {
                                            DateTime = dateTime,
                                            Directory = directory,
                                            Notes = notes,
                                            Devices = devices,
                                            imageDirectory = directory.CreateSubdirectory(IMAGES_DIRECTORY),
                                            serialDataDirectory = directory.CreateSubdirectory(SERIAL_DATA_DIRECTORY),
                                            videoDirectory = directory.CreateSubdirectory(VIDEOS_DIRECTORY),
                                        };
            deployment.Save();
            foreach (IDevice device in devices)
            {
                deployment.serialLogStreams.Add(device, File.Create(Path.Combine(deployment.serialDataDirectory.FullName, DateTime.Now.Ticks.ToString() + ".stream"), 1024));
                AVIWriter aw = new AVIWriter();
                aw.FrameRate = 15;
                aw.Open(Path.Combine(deployment.videoDirectory.FullName, DateTime.Now.Ticks.ToString() + ".avi"), 1360, 1024);
                deployment.videoWriters.Add(device, aw);
                device.MessageReceived += deployment.DeviceMessageReceived;
            }
            return deployment;
        }

        public void RecordFrame(IDevice device, IFrame frame)
        {
            videoWriters[device].AddFrame(frame.ToBitmap());
        }

        void DeviceMessageReceived(object sender, byte[] message)
        {
            IDevice device = sender as IDevice;
            if (sender != null)
            {
                this.serialLogStreams[device].Write(message, 0, message.Length);
            }
        }

        private void Save()
        {
            XmlWriter xmlWriter = XmlWriter.Create(Path.Combine(this.Directory.FullName, "Meta.xml"));
            xmlWriter.WriteStartElement("Deployment");
            xmlWriter.WriteElementString("DateTime", this.DateTime.ToString());
            xmlWriter.WriteElementString("Notes", Notes);
            xmlWriter.Close();
        }

        internal static IDeployment Load(DirectoryInfo directory)
        {
            XmlReader xmlReader = XmlReader.Create(Path.Combine(directory.FullName, "Meta.xml"));
            xmlReader.Read();
            xmlReader.ReadToDescendant("DateTime");
            DateTime dateTime = xmlReader.ReadContentAsDateTime();
            xmlReader.ReadToNextSibling("Notes");
            string notes = xmlReader.ReadContentAsString();
            xmlReader.Close();
            return Deployment.Create(dateTime, directory, notes, null);
        }

        public void Dispose()
        {
            foreach (IDevice device in Devices)
            {
                videoWriters[device].Dispose();
            }
            foreach (Stream stream in this.serialLogStreams.Values)
            {
                stream.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }
}
