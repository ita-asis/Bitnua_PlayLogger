using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayLogger
{
    public static class ExtensionMethods
    {
        public static void Add<T>(this Collection<T> collection, IEnumerable<T> items)
        {
            if (items != null)
            {
                foreach (T item in items)
                {
                    collection.Add(item);
                }
            }
        }
    }
}
