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
        private static Config m_instance;
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

        public static Config Instance
        {
            get
            {
                if (m_instance == null)
                    m_instance = new Config();
                return m_instance;
            }
        }
        public void Set(string key, string value)
        {
            //return;

            cfg.AppSettings.Settings[key].Value = value;
            cfg.Save();


            //Configuration config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);
            //config.AppSettings.Settings[key].Value = value;
            //config.Save(ConfigurationSaveMode.Modified);
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
