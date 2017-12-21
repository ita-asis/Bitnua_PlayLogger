using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExtendedGrid.Classes
{
    public class FilterParam
    {
        public string FilterExpression { get; set; }


        private Dictionary<Tuple<string, string>, List<string>> m_IndexedColumnFilter = new Dictionary<Tuple<string, string>, List<string>>();
        public Dictionary<Tuple<string, string>, List<string>> IndexedColumnFilter
        {
            get { return m_IndexedColumnFilter; }
            set
            {
                m_IndexedColumnFilter = value;
            }
        }
        

        private List<string> m_FilteredColumns = new List<string>();
        public List<string> FilteredColumns 
        {
            get { return m_FilteredColumns; }
            set { m_FilteredColumns= value; }
        }
        
    }
}
