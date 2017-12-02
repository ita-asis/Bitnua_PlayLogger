using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace PlayLogger
{
    public class PlayHistorySettings
    {
        public List<string> Files { get; set; }

        public bool ReadFileListOnly { get { return Files != null && Files.Any(); } }

        public string LastPlayedXmlDir
        {
            get { return UserSettings.Get("LastPlayedXmlDir"); }
            set { UserSettings.Set("LastPlayedXmlDir", value); }
        }

        public string PlayLocation
        {
            get { return UserSettings.Get("PlayLocation"); }
            set { UserSettings.Set("PlayLocation", value); }
        }

        public bool IsSettingEditable
        {
            get
            {
                bool value = Convert.ToBoolean(UserSettings.Get("IsSettingEditable"));

                return value || string.IsNullOrWhiteSpace(LastPlayedXmlDir) || string.IsNullOrWhiteSpace(PlayLocation);
            }
            set { UserSettings.Set("IsSettingEditable", value); }
        }

        public bool CanDeleteSongs
        {
            get { return Convert.ToBoolean(UserSettings.Get("CanDeleteSongs")); }
            set { UserSettings.Set("CanDeleteSongs", value); }
        }

        public double ViewScale
        {
            get { return Convert.ToDouble(UserSettings.Get("ViewScale")); }
            set { UserSettings.Set("ViewScale", value); }
        }



    }
}
