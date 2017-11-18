using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace PlayLogger
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            Songs = null;
            Settings = new PlayHistorySettings();
            loadDataAsync();
        }

        private void loadDataAsync()
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (object o, DoWorkEventArgs args) =>
            {
                args.Result = DbHandler.GetHistoryFromDb();

            };
            bw.RunWorkerCompleted += (object o, RunWorkerCompletedEventArgs args) =>
            {
                saveColInfo();
                Songs = null;
                Songs = new ObservableCollection<SongInfo>(args.Result as List<SongInfo>);
                reloadColInfo();
            };
            bw.RunWorkerAsync();
        }

        private void loadData()
        {
            saveColInfo();
            Songs = null;
            Songs = new ObservableCollection<SongInfo>(DbHandler.GetHistoryFromDb());
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

        public bool IsDbConnectionOn { get { return DBConnection.Instance().IsConnect(); } }


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

        private object m_LockObj = new object();
        public void Update()
        {
            OnPropertyChanged(() => IsDbConnectionOn);
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (object o, DoWorkEventArgs args) =>
            {
                lock (m_LockObj)
                {
                    DbHandler.LogSongInfo(Settings);
                    loadData();
                }

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


        private List<ColumnInfo> m_ColInfo;
        private ObservableCollection<ColumnInfo> m_ColumnInfo;
        public ObservableCollection<ColumnInfo> ColumnInfo
        {
            get { return m_ColumnInfo; }
            set
            {
                m_ColumnInfo = value;
                OnPropertyChanged(() => ColumnInfo);
            }
        }
        private void reloadColInfo()
        {
            if (m_ColInfo != null)
            {
                //System.Threading.Thread.Sleep(5000);
                ColumnInfo = new ObservableCollection<ColumnInfo>(m_ColInfo);
            }
        }

        private void saveColInfo()
        {
            if (ColumnInfo != null)
            {
                m_ColInfo = ColumnInfo.ToList();
            }
        }

    }
}
