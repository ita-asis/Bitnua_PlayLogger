using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PlayLogger.Wpf
{
    
    public class FileSystemMonitor : IDisposable
    {
        private List<string> filePaths;
        private ReaderWriterLockSlim rwlock;
        private System.Timers.Timer processTimer;
        private FileSystemWatcherArgs args;
        private FileSystemWatcher watcher;

        public event Action<IEnumerable<string>> FilesChanged;
        public void OnFilesChanged(IEnumerable<string> files)
        {
            if (FilesChanged != null)
            {
                FilesChanged.Invoke(files);
            }
        }

        public FileSystemMonitor(FileSystemWatcherArgs args)
        {
            filePaths = new List<string>();
            rwlock = new ReaderWriterLockSlim();
            this.args = args;
            InitFileSystemWatcher();
        }

        private void InitFileSystemWatcher()
        {
            watcher = new FileSystemWatcher();
            watcher.Filter = args.Filter;
            watcher.Path = args.Path;
            watcher.NotifyFilter = args.NotifyFilter;
            watcher.Changed += Watcher_FileChanged;
            watcher.Error += Watcher_Error;
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;
        }

        private void Watcher_Error(object sender, ErrorEventArgs e)
        {
            // Watcher crashed. Re-init.
            InitFileSystemWatcher();
        }

        private void Watcher_FileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                rwlock.EnterWriteLock();
                filePaths.Add(e.FullPath);

                if (processTimer == null)
                {
                    // First file, start timer.
                    processTimer = new System.Timers.Timer(2000);
                    processTimer.Elapsed += ProcessQueue;
                    processTimer.Start();
                }
                else
                {
                    // Subsequent file, reset timer.
                    processTimer.Stop();
                    processTimer.Start();
                }
            }
            finally
            {
                rwlock.ExitWriteLock();
            }
        }

        private void ProcessQueue(object sender, System.Timers.ElapsedEventArgs args)
        {
            try
            {
                rwlock.EnterReadLock();
                OnFilesChanged(filePaths.ToList());
                filePaths.Clear();
            }
            finally
            {
                if (processTimer != null)
                {
                    processTimer.Stop();
                    processTimer.Dispose();
                    processTimer = null;
                }
                rwlock.ExitReadLock();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (rwlock != null)
                {
                    rwlock.Dispose();
                    rwlock = null;
                }
                if (watcher != null)
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                    watcher = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

    } 
}
