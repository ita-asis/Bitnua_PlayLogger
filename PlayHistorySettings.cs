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
            get { return Config.Instance.Get("LastPlayedXmlDir"); }
            set { Config.Instance.Set("LastPlayedXmlDir", value); }
        }

        public string PlayLocation
        {
            get { return Config.Instance.Get("PlayLocation"); }
            set { Config.Instance.Set("PlayLocation", value); }
        }

    }
}
