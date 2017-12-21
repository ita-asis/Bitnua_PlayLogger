using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PlayLogger
{
    public class Config
    {
        private System.Configuration.Configuration cfg;
        private Config()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            string appPath = assembly.Location;
            string configFile = string.Format("{0}{1}", appPath, ".config");
            ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap();
            configFileMap.ExeConfigFilename = configFile;
            cfg = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);

        }

        private static Config s_instance;
        private static object s_lock = new object();
        public static Config Instance
        {
            get
            {
                if (s_instance == null)
                {
                    lock (s_lock)
                    {
                        if (s_instance == null)
                        {
                            s_instance = new Config();
                        }
                    }
                }

                return s_instance;
            }
        }
        public void Set(string key, string value)
        {
            cfg.AppSettings.Settings[key].Value = value;
            cfg.Save();
        }


        public string Get(string key)
        {
            var item = cfg.AppSettings.Settings[key];
            if (item != null)
            {
                return item.Value;
            }
            else
            {
                return null;
            }
        }
    }
}
