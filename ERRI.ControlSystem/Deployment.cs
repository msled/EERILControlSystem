using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Xml;

namespace EERIL.ControlSystem {
	public class Deployment : IDeployment {
		private const string IMAGES_DIRECTORY = "Images";
		private const string SERIAL_DATA_DIRECTORY = "SerialData";
		private const string VIDEOS_DIRECTORY = "Videos";

		private DirectoryInfo videoDirectory;
		private DirectoryInfo imageDirectory;
		private DirectoryInfo serialDataDirectory;
		private IList<IDevice> devices;

		public DateTime DateTime {
			get;
			private set;
		}

		public DirectoryInfo Directory {
			get;
			private set;
		}


		public string Notes {
			get;
			set;
		}

		public IList<IDevice> Devices {
			get { return devices; }
			private set { devices = value; }
		}

		private Deployment() { }

		public static IDeployment Create(DateTime dateTime, DirectoryInfo directory, string notes, IList<IDevice> devices) {
			Deployment deployment = new Deployment
			                        	{
			                        		DateTime = dateTime,
			                        		Directory = directory,
			                        		Notes = notes,
											Devices = devices,
			                        		imageDirectory = directory.CreateSubdirectory(IMAGES_DIRECTORY),
			                        		serialDataDirectory = directory.CreateSubdirectory(SERIAL_DATA_DIRECTORY),
			                        		videoDirectory = directory.CreateSubdirectory(VIDEOS_DIRECTORY)
			                        	};
			deployment.Save();
			return deployment;
		}

		private void Save() {
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
	}
}
