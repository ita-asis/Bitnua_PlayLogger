using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlayLogger.Properties
{
    [Serializable]
    public class WindowLocationSettings
    {
        public double Top { get; set; }

        public double Left { get; set; }

        public double Height { get; set; }

        public double Width { get; set; }

        public bool Maximized { get; set; }
        public WindowLocationSettings()
        {

        }
    }
}
