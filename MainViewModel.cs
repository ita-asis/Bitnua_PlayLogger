using ExtendedGrid.Classes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace PlayLogger
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            Songs = null;
            Settings = new PlayHistorySettings();
            Update();

            r_UpdateTimer = new Timer(new TimeSpan(0, 1, 0).TotalMilliseconds);
            r_UpdateTimer.Elapsed += (object sender, ElapsedEventArgs e) => Update();
            r_UpdateTimer.Start();
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
        private readonly Timer r_UpdateTimer;

        private void loadData()
        {
            saveColInfo();
            var data = DbHandler.GetHistoryFromDb();
            if (Songs == null)
            {
                Songs = new ObservableCollection<SongInfo>(data);
            }
            else
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() =>
                    {
                        Songs.Clear();
                        Songs.Add(data);
                    }));
            }

            reloadColInfo();
        }

        private ObservableCollection<SongInfo> m_Songs;
        public ObservableCollection<SongInfo> Songs
        {
            get { return m_Songs; }
            set
            {
                m_Songs = value;
                OnPropertyChanged(() => Songs);
                OnPropertyChanged(() => ColumnInfo);
            }
        }

        public bool IsDbConnectionOn
        {
            get
            {
                bool res = false;
                using (var con = new DBConnection())
                {
                    res = con.IsConnect();
                }
                return res;
            }
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
        public void Update()
        {
            IsLoading = true;
            OnPropertyChanged(() => IsDbConnectionOn);
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (object o, DoWorkEventArgs args) =>
            {
                lock (m_LockObj)
                {
                    DbHandler.LogSongInfo(Settings);
                    loadData();
                }
                IsLoading = false;
            };

            bw.RunWorkerAsync();
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


        FileSystemWatcher watcher = null;
        public void StartMonitoringXmlDir()
        {
            if (!Directory.Exists(Settings.LastPlayedXmlDir))
            {
                return;
            }

            watcher = new FileSystemWatcher();
            watcher.Path = Settings.LastPlayedXmlDir;
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Filter = "*.xml";
            watcher.Changed += new FileSystemEventHandler(OnXmlDirChanged);
            watcher.EnableRaisingEvents = true;
            IsMonitoringXmlDir = true;
        }

        private void OnXmlDirChanged(object sender, FileSystemEventArgs e)
        {
            Update();
        }
        public void StopMonitoringXmlDir()
        {
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Changed -= new FileSystemEventHandler(OnXmlDirChanged);
                watcher.Dispose();
                IsMonitoringXmlDir = false;
            }
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

        internal static void LogException(Exception exception)
        {
            var ex = exception;
            while (ex != null)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
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
            var songsToDelete = ((IList)obj).Cast<SongInfo>().ToList();
            DbHandler.RemoveSongsFromDb(songsToDelete);
            foreach (var song in songsToDelete)
            {
                Songs.Remove(song);
            }
        }

        private bool canDeleteSongs(object obj)
        {
            return obj != null && ((IList)obj).Count > 0 && Convert.ToBoolean(Config.Instance.Get("CanDeleteSongs"));
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
