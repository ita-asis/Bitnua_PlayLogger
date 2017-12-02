using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PlayLogger
{
    public class UserSettings
    {

        private UserSettings()
        {
            if (Properties.Settings.Default.UpgradeRequired)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeRequired = false;
                Properties.Settings.Default.Save();
            }
        }

        private static UserSettings m_instance;
        public static UserSettings Instance
        {
            get
            {
                if (m_instance == null)
                    m_instance = new UserSettings();
                return m_instance;
            }
        }

        public static void Set(string key, object value)
        {
            Instance.SetValue(key, value);
        }

        public void SetValue(string key, object value)
        {
            Properties.Settings.Default[key] = value;
            Properties.Settings.Default.Save();
        }


        public static string Get(string key)
        {
            return Instance.GetValue(key);
        }
        public string GetValue(string key)
        {
            var item = Properties.Settings.Default[key];
            if (item != null)
            {
                return Convert.ToString(item);
            }
            else
            {
                return null;
            }
        }
    }
}
