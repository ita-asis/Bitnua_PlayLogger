using Squirrel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PlayLogger
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static AppUpdateManager updateManager;
        [STAThread]
        public static void Main()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            // Use SecurityProtocolType.Ssl3 if needed for compatibility reasons

            updateManager = AppUpdateManager.Instance;

            var application = new App();
            application.InitializeComponent();
            application.Run();
        }
    }
}
