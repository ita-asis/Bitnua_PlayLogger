using Squirrel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PlayLogger
{
    public class AppUpdateManager
    {
        private static AppUpdateManager s_instance;
        private static object s_lock = new object();
        private AppUpdateManager()
        {
            AppDomain.CurrentDomain.ProcessExit += (object sender, EventArgs e) => DisposeUpdateManager();
            checkForUpdates();
        }
        
        public static AppUpdateManager Instance
        {
            get
            {
                if (s_instance == null)
                {
                    lock (s_lock)
                    {
                        if (s_instance == null)
                        {
                            s_instance = new AppUpdateManager();
                        }
                    }
                }

                return s_instance;
            }
        }
        
        private static async void checkForUpdates()
        {
            try
            {
                using (var mgr = getUpdateManager())
                {
                    UpdateManager = mgr.Result;
                    var updateTask = UpdateManager.UpdateApp(OnVersionUpdateProgressChanged);
                    var completedTask = updateTask.ContinueWith((e) => {
                        if (e.Result != null)
                        {
                            UpdateManager.RestartApp(); 
                        }
                    });
                    await updateTask;
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

        
        public static event Action<int> VersionUpdateProgressChanged;
        public static void OnVersionUpdateProgressChanged(int progress)
        {
            if (VersionUpdateProgressChanged != null)
            {
                VersionUpdateProgressChanged.Invoke(progress);
            }
        }
    }
}
