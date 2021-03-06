using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace PlayLogger
{
    public class PlayHistorySettings : ViewModelBase
    {
        public IEnumerable<string> Files { get; set; }

        public bool ReadFileListOnly { get { return Files != null && Files.Any(); } }

        public string LastPlayedXmlDir
        {
            get { return Convert.ToString(UserSettings.Get("LastPlayedXmlDir")); }
            set
            {
                UserSettings.Set("LastPlayedXmlDir", value);
                OnPropertyChanged(() => LastPlayedXmlDir);
            }
        }

        public string PlayLocation
        {
            get { return Convert.ToString(UserSettings.Get("PlayLocation")); }
            set
            {
                UserSettings.Set("PlayLocation", value);
                OnPropertyChanged(() => PlayLocation);
            }
        }

        public string SongFieldsToSave
        {
            get { return Convert.ToString(UserSettings.Get("SongFieldsToSave")); }
            set
            {
                UserSettings.Set("SongFieldsToSave", value);
                DbHandler.ResetSongFields();
                OnPropertyChanged(nameof(SongFieldsToSave));
            }
        }


        public bool IsSettingEditable
        {
            get
            {
                bool value = Convert.ToBoolean(UserSettings.Get("IsSettingEditable"));
                bool confVal = Convert.ToBoolean(Config.Instance.Get("IsSettingEditable"));

                return confVal || value || string.IsNullOrWhiteSpace(LastPlayedXmlDir) || string.IsNullOrWhiteSpace(PlayLocation);
            }
            set
            {
                UserSettings.Set("IsSettingEditable", value);
                OnPropertyChanged(() => IsSettingEditable);
            }
        }

        public bool CanDeleteSongs
        {
            get { return Convert.ToBoolean(UserSettings.Get("CanDeleteSongs")); }
            set
            {
                UserSettings.Set("CanDeleteSongs", value);
                OnPropertyChanged(() => CanDeleteSongs);
            }
        }

        public bool ShowLast24Hours
        {
            get { return Convert.ToBoolean(UserSettings.Get(nameof(ShowLast24Hours))); }
            set
            {
                UserSettings.Set(nameof(ShowLast24Hours), value);
                OnPropertyChanged(nameof(ShowLast24Hours));
            }
        }

        public double ViewScale
        {
            get { return Convert.ToDouble(UserSettings.Get("ViewScale")); }
            set
            {
                UserSettings.Set("ViewScale", value);
                OnPropertyChanged(() => ViewScale);
            }
        }

        public string LogoImgUrl
        {
            get { return Convert.ToString(UserSettings.Get("LogoUrl")); }
            set
            {
                UserSettings.Set("LogoUrl", value);
                OnPropertyChanged(() => LogoImgUrl);
            }
        }

        public string AmpsDB
        {
            get { return Convert.ToString(UserSettings.Get(nameof(AmpsDB))); }
            set
            {
                UserSettings.Set(nameof(AmpsDB), value);
                OnPropertyChanged(nameof(AmpsDB));
            }
        }
    }
}
