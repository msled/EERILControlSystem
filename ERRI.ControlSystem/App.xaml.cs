using System;
using System.Configuration;
using System.Linq;
using EERIL.ControlSystem.Avt;

namespace EERIL.ControlSystem {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App {
		private readonly IDeviceManager deviceManager = new DeviceManager();

		public IMissionList Missions {
			get{
				return MissionList.Current;
			}
		}

		public IDeviceManager DeviceManager
		{
			get { return deviceManager; }
		}

		public void UpdateAppSetting(string setting, String value) {
			Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

			if(config.AppSettings.Settings.AllKeys.Contains(setting)){
				config.AppSettings.Settings[setting].Value = value;
			} else {
				config.AppSettings.Settings.Add(setting, value);
			}
			config.Save(ConfigurationSaveMode.Modified);
			ConfigurationManager.RefreshSection("appSettings");
		}
	}
}
