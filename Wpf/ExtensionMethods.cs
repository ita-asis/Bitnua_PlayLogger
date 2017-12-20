using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dynamitey;
using System.Dynamic;

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

        public static IEnumerable<ExpandoObject> ToDynmicObjectList(this IList<SongInfo> list)
        {
            if (list.Count == 0)
            {
                yield break;
            }

            var browsableProps = typeof(SongInfo).GetProperties().Where(pi => pi.GetCustomAttributes(typeof(BrowsableAttribute), true).Contains(BrowsableAttribute.Yes));
            var extraFields = list.SelectMany(song => song.Fields.Keys).Distinct();

            foreach (var song in list)
            {
                dynamic obj = new ExpandoObject();

                foreach (var prop in browsableProps)
                {
                    Dynamic.InvokeSet(obj, prop.Name, prop.GetValue(song));
                }
                foreach (var key in extraFields)
                {
                    object value = null;
                    if (song.Fields.ContainsKey(key))
                    {
                        value = song.Fields[key];
                    }

                    Dynamic.InvokeSet(obj, key, value);
                }

                yield return obj;
            }
        }

        public static DataTable ToDataTable(this IList<SongInfo> list)
        {
            DataTable result = new DataTable();
            if (list.Count == 0)
                return result;

            var browsableProps = typeof(SongInfo).GetProperties().Where(pi => pi.GetCustomAttributes(typeof(BrowsableAttribute), true).Contains(BrowsableAttribute.Yes));
            var columnNames = list.SelectMany(song => song.Fields.Keys).Distinct().Concat(browsableProps.Select(p => p.Name)); ;

            result.Columns.AddRange(columnNames.Select(c => new DataColumn(c)).ToArray());
            foreach (var song in list)
            {
                var row = result.NewRow();
                foreach (var prop in browsableProps)
                {
                    row[prop.Name] = prop.GetValue(song);
                }
                foreach (var key in song.Fields.Keys)
                {
                    row[key] = song.Fields[key];
                }

                result.Rows.Add(row);
            }

            return result;
        }
    }
}
