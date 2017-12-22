using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlayLogger
{
    public class FileSystemWatcherArgs
    {
        public string Path { get; set; }

        public System.IO.NotifyFilters NotifyFilter { get; set; }

        public string Filter { get; set; }
    }
}
