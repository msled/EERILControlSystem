using System;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using EERIL.ControlSystem.Properties;
using EERIL.ControlSystem;

namespace EERIL.ControlSystem {
	internal sealed class MissionList : ThreadObservableCollection<IMission>, IMissionList {
		private static MissionList current = null;
		private DirectoryInfo missionDirectory;

		public static MissionList Current{
			get{
				if(current == null){
					current = new MissionList();
				}
				return current;
			}
		}

		public DirectoryInfo MissionDirectory {
			get {
				return missionDirectory;
			}
			set {
				missionDirectory = value;
				if (!value.Exists) {
					value.Create();
				}

				if (Settings.Default.MissionDirectory != value.FullName) {
					Settings.Default.MissionDirectory = value.FullName;
					Settings.Default.Save();
				}
				RefreshList();
			}
		}

		private MissionList() {
			MissionDirectory = new DirectoryInfo(Settings.Default.MissionDirectory);
		}

		public IMission Create(string name) {
			IMission mission = null;
			DirectoryInfo missionDirectory = new DirectoryInfo(Path.Combine(this.missionDirectory.FullName, name));
			if (!missionDirectory.Exists) {
				missionDirectory.Create();
				mission = new Mission(name, missionDirectory);
				this.Add(mission);
			} else {
				foreach (IMission existing in this) {
					if (existing.Name == name) {
						mission = existing;
						break;
					}
				}
			}
			return mission;
		}

		public void RefreshList() {
			this.Clear();
			foreach (DirectoryInfo directory in missionDirectory.GetDirectories()) {
				this.Add(new Mission(directory.Name, directory));
			}
		}
	}
}
