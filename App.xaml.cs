using Squirrel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
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
        [STAThread]
        public static void Main()
        {
            AppDomain.CurrentDomain.ProcessExit += (object sender, EventArgs e) => DisposeUpdateManager();

            checkForUpdates();
            var application = new App();
            application.InitializeComponent();
            application.Run();
        }

        private static async void checkForUpdates()
        {
            try
            {

                using (var mgr = getUpdateManager())
                {
                    UpdateManager = mgr.Result;
                    await UpdateManager.UpdateApp();
                }
            }
            catch (Exception ex)
            {
                MainViewModel.LogException(ex);
            }
            
        }

        private static async Task<Squirrel.UpdateManager> getUpdateManager()
        {
#if DEBUG
            return await Task.Run<UpdateManager>(() => new UpdateManager(@"C:\Users\iasis\Documents\Visual Studio 2013\Projects\PlayLogger\Releases"));
#else
            return await UpdateManager.GitHubUpdateManager("https://github.com/ita-asis/Bitnua_PlayLogger");
#endif
        }




        private static int _isUpdateManagerDisposed = 1;
        internal static void DisposeUpdateManager()
        {
            WaitForCheckForUpdateLockAcquire();

            if (1 == Interlocked.Exchange(ref _isUpdateManagerDisposed, 0))
            {
                if (UpdateManager != null)
                {
                    UpdateManager.Dispose();
                }
            }
        }

        /// <summary>
        /// Workaround for exception throw on SingleGlobalInstance destructor call.
        /// Before app close we should wait for 2 seconds before SingleGlobalInstance will be disposed
        /// </summary>
        private static void WaitForCheckForUpdateLockAcquire()
        {
            var goTime = _lastUpdateCheckDateTime + TimeSpan.FromMilliseconds(2000);
            var timeToWait = goTime - DateTime.Now;
            if (timeToWait > TimeSpan.Zero)
                Thread.Sleep(timeToWait);
        }
        private static DateTime _lastUpdateCheckDateTime = DateTime.Now - TimeSpan.FromDays(1);
        private static UpdateManager UpdateManager;

        private static async Task<UpdateInfo> CheckForUpdate(bool ignoreDeltaUpdates)
        {
            _lastUpdateCheckDateTime = DateTime.Now;
            return await UpdateManager.CheckForUpdate(ignoreDeltaUpdates);
        }
    }
}
