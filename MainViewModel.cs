using Dynamitey;
using ExtendedGrid.Classes;
using PlayLogger.Wpf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;

namespace PlayLogger
{
    public static class TimerExtenstion
    {
        public static void Reset(this Timer timer)
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Start();
            }
        }
    }

    public class MainViewModel : ViewModelBase
    {
        private Timer m_UpdateTimer;
        private Timer m_CheckConnectionTimer;

        private MainViewModel()
        {
            listenToAppUpdate();
            setSongs(null);
            Settings = new PlayHistorySettings();
            DbHandler.RemoveOldSongs();

            var updateTask = Update();

            Settings.PropertyChanged += restartMonitor;
            StartMonitoringXmlDir();
            startTimers();
            var logTask = DbHandler.LogAppInfoAsync(Settings);
        }

        private void listenToAppUpdate()
        {
            AppUpdateManager.VersionUpdateProgressChanged += (progress) =>
            {
                AppUpdateProgress = progress;
            };
        }


        private int m_AppUpdateProgress;
        public int AppUpdateProgress
        {
            get { return m_AppUpdateProgress; }
            set
            {
                m_AppUpdateProgress = value;
                if (AppUpdateProgress > 0 && AppUpdateProgress < 99)
                {
                    UpdateVersionText = string.Format("Updating app... progress: {0}", AppUpdateProgress);
                }
                else
                {
                    UpdateVersionText = null;
                }

                OnPropertyChanged(() => AppUpdateProgress);
            }
        }


        private string m_UpdateVersionText;
        public string UpdateVersionText
        {
            get { return m_UpdateVersionText; }
            set
            {
                m_UpdateVersionText = value;
                OnPropertyChanged(() => UpdateVersionText);
            }
        }

        private DateTime m_LastUpdateDate;
        public DateTime LastUpdateDate
        {
            get { return m_LastUpdateDate; }
            set
            {
                m_LastUpdateDate = value;
                OnPropertyChanged(() => LastUpdateDate);
            }
        }

        private void startTimers()
        {
            startUpdateTimer();
            startConnectionCheckTimer();
        }

        private void startConnectionCheckTimer()
        {
            m_CheckConnectionTimer = new Timer(new TimeSpan(0, 1, 0).TotalMilliseconds);
            m_CheckConnectionTimer.Elapsed += (object sender, ElapsedEventArgs e) => checkDbConn();
            m_CheckConnectionTimer.Start();
        }

        private void startUpdateTimer()
        {
            m_UpdateTimer = new Timer(new TimeSpan(0, getTimerMinutes(), 0).TotalMilliseconds);
            m_UpdateTimer.Elapsed += (object sender, ElapsedEventArgs e) => Update();
            m_UpdateTimer.Start();
        }


        private static MainViewModel s_Instance = null;
        private static object s_ctorLock = new object();


        public static MainViewModel Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    lock (s_ctorLock)
                    {
                        if (s_Instance == null)
                        {
                            s_Instance = new MainViewModel();
                        }
                    }
                }

                return s_Instance;
            }
        }
        private static int getTimerMinutes()
        {
            int min = Convert.ToInt32(Config.Instance.Get("auto_refreosh_min"));
            if (min == 0)
            {
                min = 10;
            }

            return min;
        }

        private Dictionary<string, string> m_ColumnHeaders;
        public Dictionary<string, string> ColumnHeaders
        {
            get
            {
                if (m_ColumnHeaders == null)
                {
                    m_ColumnHeaders = new Dictionary<string, string>();
                    m_ColumnHeaders.Add("Id", "ID");
                    m_ColumnHeaders.Add("Title", "שם ריקוד");
                    m_ColumnHeaders.Add("PlayTime", "נוגן ב");
                    m_ColumnHeaders.Add("PlayLocation", "רחבה");
                    m_ColumnHeaders.Add("Fields[Type]", "סוג");
                }

                return m_ColumnHeaders;
            }
        }

        private void loadData()
        {
            saveColInfo();
            var data = DbHandler.GetHistoryFromDb(ShowLast24Hours ? DateTime.Now.AddHours(-24) : (DateTime?)null);
            setSongs(data);
            reloadColInfo();
        }

        private IEnumerable<SongInfo> m_Songs;
        public IEnumerable<SongInfo> SongsInfo => m_Songs;

        private Wpf.DynamicObjectBindingList m_SongsDynamic;
        public object Songs => m_SongsDynamic;

        public void setSongs(IList<SongInfo> songs)
        {
            m_Songs = songs;
            if (songs != null)
            {
                m_SongsDynamic = new Wpf.DynamicObjectBindingList(songs.ToDynmicObjectList().ToList(), typeof(SongInfo));
            }
            OnPropertyChanged(() => Songs);
            OnPropertyChanged(() => ColumnInfo);
            OnPropertyChanged(() => TotalCountLbl);
        }

        public string TotalCountLbl => $"סה\"כ: {(m_Songs != null ? m_Songs.Count() : 0)}";

        private bool m_IsDbConnectionOn;
        public bool IsDbConnectionOn
        {
            get
            {
                return m_IsDbConnectionOn;
            }
            private set
            {
                m_IsDbConnectionOn = value;
                OnPropertyChanged(() => IsDbConnectionOn);
            }
        }

        private async Task<bool> checkDbConn()
        {
            bool res = false;
            await Task.Run(() =>
            {
                using (var con = MyDbConnectionBase.CreateInstace())
                {
                    res = con.IsConnect();
                }
            });

            IsDbConnectionOn = res;
            return res;
        }


        private PlayHistorySettings m_Settings;
        public PlayHistorySettings Settings
        {
            get { return m_Settings; }
            set
            {
                m_Settings = value;
                OnPropertyChanged(() => Settings);
            }
        }


        private bool m_IsLoading;
        public bool IsLoading
        {
            get { return m_IsLoading; }
            set
            {
                m_IsLoading = value;
                OnPropertyChanged(() => IsLoading);
            }
        }
        private object m_LockObj = new object();
        public async Task Update(PlayHistorySettings args = null)
        {
            if (args == null)
            {
                args = Settings;
            }

            Debug.WriteLine("running Update...");
            IsLoading = true;
            bool isDbConOpen = await checkDbConn();
            if (isDbConOpen)
            {
                await Task.Run(() =>
                {
                    lock (m_LockObj)
                    {
                        DbHandler.LogSongInfo(args);
                        loadData();
                        Debug.WriteLine("running Update complete");
                        LastUpdateDate = DateTime.Now;
                        m_UpdateTimer.Reset();
                    }
                });
            }
            IsLoading = false;
        }

        private bool m_IsMonitoringXmlDir;
        public bool IsMonitoringXmlDir
        {
            get { return m_IsMonitoringXmlDir; }
            set
            {
                m_IsMonitoringXmlDir = value;
                OnPropertyChanged(() => IsMonitoringXmlDir);
            }
        }

        FileSystemMonitor monitor;
        public void StartMonitoringXmlDir()
        {
            if (!Directory.Exists(Settings.LastPlayedXmlDir))
            {
                return;
            }

            var watcherArgs = new FileSystemWatcherArgs()
            {
                Path = Settings.LastPlayedXmlDir,
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = "*.xml"
            };
            monitor = new FileSystemMonitor(watcherArgs);
            monitor.FilesChanged += monitor_FilesChanged;

            IsMonitoringXmlDir = true;
        }

        private async void monitor_FilesChanged(IEnumerable<string> files)
        {
            Debug.WriteLine(MethodBase.GetCurrentMethod().Name);
            PlayHistorySettings args = new PlayHistorySettings()
            {
                Files = files,
                PlayLocation = Settings.PlayLocation
            };

            await Update(args);
        }

        private void restartMonitor(object sender, PropertyChangedEventArgs e)
        {
            if (IsMonitoringXmlDir && e.PropertyName == nameof(Settings.LastPlayedXmlDir))
            {
                StopMonitoringXmlDir();
                StartMonitoringXmlDir();
            }
        }

        public void StopMonitoringXmlDir()
        {
            if (monitor != null)
            {
                monitor.Dispose();
            }
            IsMonitoringXmlDir = false;
        }

        internal void ToggleXmlDirMonitor()
        {
            if (IsMonitoringXmlDir)
            {
                StopMonitoringXmlDir();
            }
            else
            {
                StartMonitoringXmlDir();
            }
        }

        internal async Task UpdateLastPlayedOnAmpsDB(IEnumerable<SongInfo> songs)
        {
            if (string.IsNullOrEmpty(Settings.AmpsDB) ||
                !File.Exists(Settings.AmpsDB) ||
                songs == null)
                return;

            IsLoading = true;
            await Task.Run(() => updateLastPlayed(songs));
            IsLoading = false;
        }

        private void updateLastPlayed(IEnumerable<SongInfo> songs)
        {
            OleDbConnectionStringBuilder b = new OleDbConnectionStringBuilder()
            {
                //Provider = "Microsoft.Jet.OLEDB.4.0",
                Provider = "Microsoft.ACE.OLEDB.12.0",
                DataSource = Settings.AmpsDB,
                PersistSecurityInfo = false
            };

            using (var connection = new OleDbConnection(b.ConnectionString))
            {
                connection.Open();
                var query = "Update Songs SET SongLastPlayed = @lastPlayed WHERE ID = @songID AND SongLastPlayed < @lastPlayed";

                using (OleDbCommand cmd = connection.CreateCommand())
                {
                    cmd.CommandText = query;

                    foreach (var song in songs)
                    {
                        if (song == null)
                            continue;

                        cmd.Parameters.Clear();
                        cmd.Parameters.Add(new OleDbParameter("@lastPlayed", song.PlayTime) { OleDbType = OleDbType.Date });
                        cmd.Parameters.Add(new OleDbParameter("@songID", song.Id) { DbType = DbType.Int64 });
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        internal static void LogException(Exception exception)
        {
            var ex = exception;
            while (ex != null)
            {
                Trace.WriteLine(ex.Message);
                Trace.WriteLine(ex.StackTrace);
                ex = ex.InnerException;
            }
        }


        private RelayCommand m_DeleteSongCommand;
        public ICommand DeleteSongCommand
        {
            get
            {
                if (m_DeleteSongCommand == null)
                {
                    m_DeleteSongCommand = new RelayCommand(deleteSongs, canDeleteSongs);
                }
                return m_DeleteSongCommand;
            }
        }

        private void deleteSongs(object obj)
        {
            var songsToDelete = ((IList)obj).Cast<ExpandoObject>().ToList();
            var songInfoList = songsToDelete.Select(s => new SongInfo() { RecordId = Dynamic.InvokeGet(s, "RecordId") });
            DbHandler.RemoveSongsFromDb(songInfoList);
            foreach (var song in songsToDelete)
            {
                m_SongsDynamic.Remove(song);
            }
        }

        private bool canDeleteSongs(object obj)
        {
            return obj != null && ((IList)obj).Count > 0 && Convert.ToBoolean(UserSettings.Get("CanDeleteSongs"));
        }


        private string m_ColInfo;
        private string m_ColumnInfo;
        public string ColumnInfo
        {
            get { return m_ColumnInfo; }
            set
            {
                m_ColumnInfo = value;
                OnPropertyChanged(() => ColumnInfo);
            }
        }

        private Dictionary<string, int> m_InitalColumnOrderMap;
        public Dictionary<string, int> InitalColumnOrderMap
        {
            get
            {
                if (m_InitalColumnOrderMap == null)
                {
                    var cols = new string[] { "PlayLocation", "Title", "Fields[\"Type\"]", "PlayTime" };
                    int length = cols.Length;
                    var res = new Dictionary<string, int>(length);
                    int i = 0;
                    foreach (string col in cols)
                    {
                        res.Add(col, i++);
                    }
                    m_InitalColumnOrderMap = res;
                }

                return m_InitalColumnOrderMap;
            }
        }

        private FilterParam m_FilterInfo;
        private FilterParam m_Filter;
        public FilterParam Filter
        {
            get { return m_Filter; }
            set
            {
                m_Filter = value;
                OnPropertyChanged(() => Filter);
            }
        }

        public bool ShowLast24Hours
        {
            get => Settings.ShowLast24Hours;
            set
            {
                if (Settings.ShowLast24Hours != value)
                {
                    Settings.ShowLast24Hours = value;
                    Update();
                }
            }
        }

        private void reloadColInfo()
        {
            if (m_ColInfo != null)
            {
                ColumnInfo = m_ColInfo;
            }
            if (m_FilterInfo != null)
            {
                Filter = m_FilterInfo;
            }

        }

        private void saveColInfo()
        {
            if (ColumnInfo != null)
            {
                m_ColInfo = ColumnInfo;
            }

            if (Filter != null)
            {
                m_FilterInfo = Filter;
            }
        }
    }
}
