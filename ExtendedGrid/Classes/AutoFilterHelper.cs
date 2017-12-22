using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using ExtendedGrid.ExtendedGridControl;
using ExtendedGrid.Styles;
using System.Collections;
using System.Dynamic;
using Dynamitey;

namespace ExtendedGrid.Classes
{
    internal sealed class AutoFilterHelper
    {
        public AutoFilterHelper(ExtendedDataGrid grid)
        {
            CurrentGrid = grid;
        }
        public ListBox CurrentSigmaListBox { get; set; }
        public ObservableCollection<CheckedListItem> CurrentDistictValues { get; set; }
        public ListBox CurrentListBox { get; set; }
        public string CurrentColumName { get; set; }

        public ExtendedDataGrid CurrentGrid { get; private set; }

        public string CurrentSigmaColumn { get; set; }


        private IEnumerable<CheckedListItem> orderCols(IEnumerable<CheckedListItem> i_items)
        {
            yield return new CheckedListItem { IsChecked = false, Name = "(Select All)", IsSelectAll = "(Select All)" };

            foreach (var item in i_items.OrderBy(x => x.Name))
            {
                yield return item;
            }
        }
        public ObservableCollection<CheckedListItem> GetDistictValues(DataGrid grid, string columnName)
        {
            var originalResult = _GetDistictValues(grid, columnName);

            // order values by name
            IEnumerable<CheckedListItem> orderedResult = orderCols(originalResult);

            var ditictValues = new ObservableCollection<CheckedListItem>(orderedResult);


            // check 'select all' if needed
            if (ditictValues.Count(c => c.IsChecked) == ditictValues.Count - 1)
                ditictValues[0].IsChecked = true;


            CurrentDistictValues = ditictValues;
            return ditictValues;
        }
        private IEnumerable<CheckedListItem> _GetDistictValues(DataGrid grid, string columnName)
        {
            if (grid.ItemsSource is DataView)
            {
                var itemSource = (DataView)grid.ItemsSource;
                var valuesHash = new HashSet<string>();

                DataTable table = itemSource.Table;
                List<string> filteredValues = GetFilteredColumnValues(columnName, itemSource);
                foreach (DataRow row in table.Rows)
                {
                    object value = row[columnName];
                    var strValue = Convert.ToString(value);

                    if (!valuesHash.Contains(strValue))
                    {
                        CheckedListItem currValue = null;
                        if (!string.IsNullOrEmpty(strValue))
                        {
                            currValue = new CheckedListItem { Name = value, IsChecked = filteredValues.Contains("'" + value + "'") };
                        }
                        else if (value == null)
                        {
                            currValue = new CheckedListItem { Name = null, IsChecked = filteredValues.Contains(null) };
                        }
                        else if (string.IsNullOrEmpty(strValue))
                        {
                            currValue = new CheckedListItem { Name = value, IsChecked = filteredValues.Contains("") };
                        }

                        valuesHash.Add(strValue);
                        yield return currValue;
                    }
                }

            }
            else if (CollectionViewSource.GetDefaultView(grid.ItemsSource) != null)
            {
                ICollectionView view = CollectionViewSource.GetDefaultView(grid.ItemsSource);
                _view = view;
                var unquieValues = new HashSet<string>();
                foreach (var rowData in grid.Items)
                {
                    if (columnNameHasIndexer(columnName))
                    {
                        string collectionName = getCollectionNameOfIndexer(columnName);
                        string colKey = getKeyOfIndexer(columnName);
                        var collectionProp = rowData.GetType().GetProperty(collectionName);
                        Type t = collectionProp.PropertyType;
                        bool isDict = t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>);
                        if (collectionProp != null && isDict)
                        {
                            IDictionary theCollection = (IDictionary)collectionProp.GetValue(rowData, null);
                            var data = Convert.ToString(theCollection[colKey]);
                            if (!unquieValues.Contains(data))
                            {
                                unquieValues.Add(data);
                            }
                        }
                    }
                    else
                    {
                        string data = null;
                        if (rowData.GetType() == typeof(ExpandoObject))
                        {
                            data = Convert.ToString(Dynamic.InvokeGet(rowData, columnName));
                        }
                        else
                        {
                            var propertyValue = rowData.GetType().GetProperty(columnName);
                            if (propertyValue != null)
                            {
                                data = propertyValue.GetValue(rowData, null) == null ? null : Convert.ToString(propertyValue.GetValue(rowData, null));
                            }
                        }
                        if (string.IsNullOrEmpty(data))
                        {
                            data = string.Empty;
                        }
                        if (!unquieValues.Contains(data))
                        {
                            unquieValues.Add(data);
                        }
                    }
                }

                List<string> filteredValues = string.IsNullOrEmpty(FilterExpression) ? new List<string>() : GetFilteredColumnValues(columnName, FilterExpression);
                foreach (var value in unquieValues)
                {
                    CheckedListItem currValue = null;
                    if (!string.IsNullOrEmpty(Convert.ToString(value)))
                    {
                        currValue = new CheckedListItem { Name = value, IsChecked = filteredValues.Contains("'" + value + "'") || filteredValues.Contains(value) };
                    }
                    else if (value == null)
                    {
                        currValue = new CheckedListItem { Name = null, IsChecked = filteredValues.Contains(null) };
                    }
                    else if (string.IsNullOrEmpty(Convert.ToString(value)))
                    {
                        currValue = new CheckedListItem { Name = value, IsChecked = filteredValues.Contains("") };
                    }

                    yield return currValue;
                }
            }
        }

        private static string getKeyOfIndexer(string columnName)
        {
            string colKey = columnName.Substring(columnName.IndexOf('[') + 1, columnName.IndexOf(']') - columnName.IndexOf('[') - 1);
            return colKey;
        }

        private static string getCollectionNameOfIndexer(string columnName)
        {
            string collectionName = columnName.Substring(0, columnName.IndexOf('['));
            return collectionName;
        }

        private static bool columnNameHasIndexer(string columnName)
        {
            return columnName.Contains('[') && columnName.Contains(']');
        }

        public void ApplyFilters(DataGrid grid, string columnName, object value)
        {
            registerFilteredColumns(columnName);
            value = AddEscapeSequece(value);
            if (grid.ItemsSource is DataView)
            {
                var itemSource = (DataView)grid.ItemsSource;
                if (itemSource == null) return;
                if (!string.IsNullOrEmpty(itemSource.RowFilter))
                {
                    string filter = FilterGenerator(itemSource.RowFilter, columnName, value);
                    if (filter != itemSource.RowFilter || columnNameHasIndexer(columnName))
                    {
                        itemSource.RowFilter = CorrectRowFilter(filter);
                        grid.Items.Refresh();
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(Convert.ToString(value)))
                    {
                        string dummy = value == null ? null : "";
                        switch (dummy)
                        {
                            case "":
                                itemSource.RowFilter = "[" + columnName + "]" + " = ''";
                                break;
                            case null:
                                itemSource.RowFilter = "[" + columnName + "]" + " IS Null";
                                break;
                        }
                    }
                    else
                        if (itemSource.Table.Columns[columnName].DataType.BaseType.ToString() == "System.Enum")
                        {
                            var value1 = Convert.ToInt32(Enum.Parse(itemSource.Table.Columns[columnName].DataType, Convert.ToString(value)));
                            itemSource.RowFilter = "[" + columnName + "]" + " " + " IN " + "(" + value1 + ")";
                        }
                        else
                            itemSource.RowFilter = "[" + columnName + "]" + " " + " IN " + "(" + "'" + value + "'" + ")";
                }

                int count = CurrentDistictValues.Count(c => c.IsChecked);
                if (count == CurrentDistictValues.Count - 1)
                {
                    CurrentDistictValues[0].IsChecked = true;
                    if (CurrentListBox != null)
                    {
                        CurrentListBox.ItemsSource = CurrentDistictValues;
                        CurrentListBox.UpdateLayout();
                        CurrentListBox.Items.Refresh();
                    }
                }

                if (CurrentListBox != null)
                {
                    var clearButton = FindControls.FindChild<Button>(CurrentListBox.Parent, "btnClear");
                    if (clearButton != null)
                    {
                        clearButton.IsEnabled = CurrentDistictValues.Count(c => c.IsChecked) > 0;
                    }
                }
            }
            else if (CollectionViewSource.GetDefaultView(grid.ItemsSource) != null)
            {
                _view = CollectionViewSource.GetDefaultView(grid.ItemsSource);
                if (!string.IsNullOrEmpty(FilterExpression) && FilterExpression != "True")
                {
                    string filter = FilterGenerator(FilterExpression, columnName, value);
                    if (filter != FilterExpression || columnNameHasIndexer(columnName))
                    {
                        FilterExpression = CorrectRowFilter(filter);
                        grid.Items.Refresh();
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(Convert.ToString(value)))
                    {
                        string dummy = value == null ? null : "";
                        switch (dummy)
                        {
                            case "":
                                FilterExpression = "[" + columnName + "]" + " = ''";
                                break;
                            case null:
                                FilterExpression = "[" + columnName + "]" + " IS Null";
                                break;
                        }
                    }
                    else
                    {
                        var readOnlyCollection = (((ListCollectionView)(_view))).ItemProperties;
                        if (readOnlyCollection != null)
                        {
                            if (columnNameHasIndexer(columnName))
                            {
                                addIndexedValue(columnName, value);
                                FilterExpression = "True";
                            }
                            else
                            {
                                Type t;
                                var item =
                                    readOnlyCollection.First(
                                        c => c.Name == columnName);
                                t = item.PropertyType;
                                if (t.BaseType != null && t.BaseType.ToString() == "System.Enum")
                                {
                                    var value1 = Convert.ToInt32(Enum.Parse(t, Convert.ToString(value)));
                                    FilterExpression = "[" + columnName + "]" + " " + " IN " + "(" + value1 + ")";
                                }
                                else
                                    FilterExpression = "[" + columnName + "]" + " " + " IN " + "(" + "'" + value + "'" + ")";
                            }
                        }
                    }
                }

                int count = CurrentDistictValues.Count(c => c.IsChecked);
                if (count == CurrentDistictValues.Count - 1)
                {
                    CurrentDistictValues[0].IsChecked = true;
                    if (CurrentListBox != null)
                    {
                        CurrentListBox.ItemsSource = CurrentDistictValues;
                        CurrentListBox.UpdateLayout();
                        CurrentListBox.Items.Refresh();
                    }
                }

                if (CurrentListBox != null)
                {
                    var clearButton = FindControls.FindChild<Button>(CurrentListBox.Parent, "btnClear");
                    if (clearButton != null)
                    {
                        clearButton.IsEnabled = CurrentDistictValues.Count(c => c.IsChecked) > 0;
                    }
                }
            }
        }


        private void addIndexedValue(string columnName, object value)
        {
            string collProp = getCollectionNameOfIndexer(columnName);
            string collKey = getKeyOfIndexer(columnName);
            var key = new Tuple<string, string>(collProp, collKey);

            if (Filter.IndexedColumnFilter.ContainsKey(key))
            {
                if (!Filter.IndexedColumnFilter[key].Contains(value))
                {
                    Filter.IndexedColumnFilter[key].Add(Convert.ToString(value));
                }
            }
            else
            {
                Filter.IndexedColumnFilter.Add(key, new List<string> { Convert.ToString(value) });
            }
        }

        private object AddEscapeSequece(object value)
        {
            if (string.IsNullOrEmpty(Convert.ToString(value))) return value;
            string newValue = EscapeLikeValue(value.ToString());
            return newValue;
        }

        private string FilterNullAndBlanlValues(object value, string columnName, string existingFilter,
                                                       int startIndex)
        {
            string newFilter = existingFilter.Trim();
            if (startIndex != -1)
            {
                if (Convert.ToString(value) == "" && value != null)
                {
                    //NOT TESTED
                    int startIndex1 = newFilter.IndexOf("[" + columnName + "]" + " = ''", StringComparison.Ordinal);
                    if (startIndex1 != -1)
                    {
                        int lastIndex1 = startIndex1 + Convert.ToString("[" + columnName + "]" + " = ''").Length;
                        if (lastIndex1 == newFilter.Length)
                        {
                            if (!newFilter.Contains("[" + columnName + "]" + " IS Null"))
                            {
                                newFilter = newFilter.Insert(lastIndex1, " Or ([" + columnName + "]" + " IS Null)");
                            }
                        }
                        else
                        {
                            if (!newFilter.Contains("[" + columnName + "]" + " = ''"))
                            {
                                newFilter = newFilter.Insert(lastIndex1, " Or ([" + columnName + "]" + " = '') AND");
                            }
                        }
                    }
                    else
                    {
                        //TESTED
                        string actaulFilter = newFilter.Substring(startIndex + (columnName + " " + " IN ").Length + 1);
                        int lastIndex = actaulFilter.IndexOf(")", StringComparison.Ordinal);
                        actaulFilter = actaulFilter.Substring(0, lastIndex);
                        int isNullLastIndex = newFilter.IndexOf("[" + columnName + "] IS Null)", StringComparison.Ordinal);
                        int lastIndex1 = startIndex + ("[" + columnName + "]" + " " + " IN ").Length +
                                         actaulFilter.Length;
                        if (lastIndex1 < isNullLastIndex)
                        {
                            lastIndex1 = isNullLastIndex + Convert.ToString("[" + columnName + "] IS Null)").Length;
                        }
                        if (lastIndex1 + 1 == newFilter.Length) lastIndex1 = newFilter.Length;
                        if (lastIndex1 == newFilter.Length)
                        {
                            if (!newFilter.Contains("[" + columnName + "]" + " = ''"))
                            {
                                newFilter = newFilter.Insert(lastIndex1, " Or ([" + columnName + "]" + " = '')");
                            }
                        }
                        else
                        {
                            if (!newFilter.Contains("[" + columnName + "]" + " = ''"))
                            {
                                newFilter = newFilter.Insert(lastIndex1, " Or ([" + columnName + "]" + " = '') AND");
                            }
                        }
                    }
                }
                else if (value == null)
                {
                    //NOT TESTED
                    int startIndex1 = newFilter.IndexOf("[" + columnName + "]" + " IS Null)", StringComparison.Ordinal);
                    if (startIndex1 != -1)
                    {
                        if (!newFilter.Contains("[" + columnName + "]" + " IS Null"))
                        {
                            int lastIndex1 = startIndex1 + Convert.ToString("[" + columnName + "]" + " IS Null)").Length;
                            int x = 0;
                            if (lastIndex1 == newFilter.Length)
                                x = 0;
                            else if (lastIndex1 + 1 == newFilter.Length)
                                x = 1;
                            newFilter = lastIndex1 + x == newFilter.Length
                                            ? newFilter.Insert(lastIndex1, " Or ([" + columnName + "]" + " IS Null)")
                                            : newFilter.Insert(lastIndex1, " Or ([" + columnName + "]" + " IS Null) AND");
                        }
                    }
                    else
                    {
                        //TESTED
                        if (!newFilter.Contains("[" + columnName + "]" + " IS Null"))
                        {
                            string actaulFilter =
                                newFilter.Substring(startIndex + (columnName + " " + " IN ").Length + 1);
                            int lastIndex = actaulFilter.IndexOf(")", StringComparison.Ordinal);
                            actaulFilter = actaulFilter.Substring(0, lastIndex);


                            int isNullLastIndex = newFilter.IndexOf("([" + columnName + "]" + " = '')", StringComparison.Ordinal);
                            int lastIndex1 = startIndex + ("[" + columnName + "]" + " " + " IN ").Length +
                                             actaulFilter.Length + 1;
                            if (isNullLastIndex > lastIndex1)
                            {
                                lastIndex1 = isNullLastIndex +
                                             Convert.ToString("([" + columnName + "]" + " = '')").Length;
                            }
                            if (lastIndex1 > newFilter.Length)
                                lastIndex1 = newFilter.Length;

                            newFilter = lastIndex1 == newFilter.Length
                                            ? newFilter.Insert(lastIndex1, " Or ([" + columnName + "]" + " IS Null)")
                                            : newFilter.Insert(lastIndex1, " Or ([" + columnName + "]" + " IS Null) AND");
                        }
                    }
                }
            }
            else if (newFilter.Contains("[" + columnName + "]" + " = ''"))
            {
                if (Convert.ToString(value) == "")
                {
                    //NOT TESTED
                    int startIndex1 = newFilter.IndexOf("[" + columnName + "]" + " = ''", StringComparison.Ordinal);
                    if (startIndex1 != -1)
                    {
                        int lastIndex1 = startIndex1 + Convert.ToString("[" + columnName + "]" + " = ''").Length;
                        int x = 0;
                        if (lastIndex1 == newFilter.Length)
                            x = 0;
                        else if (lastIndex1 + 1 == newFilter.Length)
                            x = 1;
                        if (!newFilter.Contains("[" + columnName + "]" + " IS Null"))
                        {
                            newFilter = lastIndex1 + x == newFilter.Length
                                            ? newFilter.Insert(lastIndex1, " Or ([" + columnName + "]" + " IS Null)")
                                            : newFilter.Insert(lastIndex1, " Or ([" + columnName + "]" + " IS Null) AND");
                        }
                    }
                }
                else
                {
                    //NOT TESTED
                    int startIndex1 = newFilter.IndexOf("[" + columnName + "]" + " = ''", StringComparison.Ordinal);
                    int startIndex2 = newFilter.IndexOf("[" + columnName + "]" + " IS Null", StringComparison.Ordinal);
                    if (startIndex2 != -1)
                    {
                        startIndex1 = Math.Min(startIndex1, startIndex2);
                    }
                    if (startIndex1 != -1)
                    {
                        newFilter = newFilter.Insert(startIndex1,
                                                     "[" + columnName + "]" + " " + " IN " + "(" + "'" + value + "'" +
                                                     ") Or ");
                    }
                }
            }
            else if (newFilter.Contains("[" + columnName + "]" + " IS Null"))
            {
                if (Convert.ToString(value) == "")
                {
                    //TESTED
                    int startIndex1 = newFilter.IndexOf("[" + columnName + "]" + " IS Null", StringComparison.Ordinal);
                    if (startIndex1 != -1)
                    {
                        int lastIndex1 = startIndex1 + Convert.ToString("[" + columnName + "]" + " IS Null").Length;
                        if (!newFilter.Contains("[" + columnName + "]" + " = ''"))
                        {
                            int x = 0;
                            if (lastIndex1 == newFilter.Length)
                                x = 0;
                            else if (lastIndex1 + 1 == newFilter.Length)
                                x = 1;
                            else if (lastIndex1 + 2 == newFilter.Length)
                                x = 2;
                            newFilter = lastIndex1 + x == newFilter.Length
                                            ? newFilter.Insert(lastIndex1, " Or ([" + columnName + "]" + " = '')")
                                            : newFilter.Insert(lastIndex1, " Or ([" + columnName + "]" + " = '') AND");
                        }
                    }
                }
                else
                {
                    //NOT TESTED
                    int startIndex1 = newFilter.IndexOf("[" + columnName + "]" + " = ''", StringComparison.Ordinal);
                    int startIndex2 = newFilter.IndexOf("[" + columnName + "]" + " IS Null", StringComparison.Ordinal);
                    if (startIndex2 != -1)
                    {
                        startIndex1 = Math.Min(startIndex1, startIndex2);
                    }
                    if (startIndex1 != -1)
                    {
                        newFilter = newFilter.Insert(startIndex1,
                                                     "[" + columnName + "]" + " " + " IN " + "(" + "'" + value + "'" +
                                                     ") Or ");
                    }
                }
            }
            else
            {
                if (Convert.ToString(value) == "" && value != null)
                {
                    newFilter = newFilter + "AND ([" + columnName + "]" + " = '')";
                }
                else if (value == null)
                {
                    newFilter = newFilter + "AND ([" + columnName + "]" + " IS Null)";
                }
            }

            return newFilter;
        }
        private string FilterGenerator(string exisitngFilter, string columnName, object value)
        {

            var isEnumType = false;
            int enumValue = -1;
            if (CurrentGrid.ItemsSource is DataView)
            {
                var dv = CurrentGrid.ItemsSource as DataView;
                var baseType = dv.Table.Columns[columnName].DataType.BaseType;
                if (baseType != null)
                {
                    isEnumType = baseType.ToString().Equals("System.Enum");
                    if (isEnumType)
                        enumValue = Convert.ToInt32(Enum.Parse(dv.Table.Columns[columnName].DataType, Convert.ToString(value)));
                }
            }
            else if (CollectionViewSource.GetDefaultView(CurrentGrid.ItemsSource) != null)
            {
                var readOnlyCollection = (((ListCollectionView)(CollectionViewSource.GetDefaultView(CurrentGrid.ItemsSource)))).ItemProperties;
                if (readOnlyCollection != null)
                {
                    if (columnNameHasIndexer(columnName))
                    {
                        addIndexedValue(columnName, value);
                    }
                    else
                    {
                        var item =
                            readOnlyCollection.First(
                                c => c.Name == columnName);
                        if (item.PropertyType.BaseType != null && item.PropertyType.BaseType.ToString() == "System.Enum")
                        {
                            isEnumType = true;
                            enumValue = Convert.ToInt32(Enum.Parse(item.PropertyType, Convert.ToString(value)));
                        }
                    }
                }
            }
            string newFilter = exisitngFilter;

            if (newFilter.Contains("[" + columnName + "]" + " " + " IN ") ||
                newFilter.Contains("[" + columnName + "]" + " = ''") ||
                newFilter.Contains("[" + columnName + "]" + " IS Null"))
            {
                int startIndex = newFilter.IndexOf("[" + columnName + "]" + " " + " IN ", StringComparison.Ordinal);
                if (startIndex != -1)
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(value)))
                    {
                        string actaulFilter =
                            newFilter.Substring(startIndex + ("[" + columnName + "]" + " " + " IN ").Length + 1);
                        int lastIndex = actaulFilter.LastIndexOf(")", StringComparison.Ordinal);
                        while (lastIndex > 0)
                        {
                            actaulFilter = actaulFilter.Substring(0, lastIndex);
                            lastIndex = actaulFilter.LastIndexOf(")", StringComparison.Ordinal);
                        }

                        string[] listOfFilter = actaulFilter.Split(',');
                        if (listOfFilter.Contains("'" + value + "'"))
                            return exisitngFilter;
                        int indexToput = startIndex + ("[" + columnName + "]" + " " + " IN ").Length +
                                         actaulFilter.Length + 1;
                        newFilter = !isEnumType ? newFilter.Insert(indexToput, "," + "'" + value + "'") : newFilter.Insert(indexToput, "," + enumValue);
                    }
                    else
                    {
                        newFilter = FilterNullAndBlanlValues(value, columnName, newFilter, startIndex);
                    }
                }
                else
                {
                    newFilter = FilterNullAndBlanlValues(value, columnName, newFilter, startIndex);
                }

                return newFilter.Trim();
            }
            newFilter = newFilter.Trim();

            if (columnNameHasIndexer(columnName)) // && newFilter.IndexOf("True AND ") == -1)
            {
                //newFilter = "True AND " + newFilter;
            }
            else
            {
                if (!string.IsNullOrEmpty(newFilter))
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(value)))
                    {
                        int idexAppliedOn = CurrentGrid.Columns.Count(gridColumn => newFilter.Contains("[" + gridColumn.SortMemberPath + "]" + " " + " IN ") || newFilter.Contains("[" + gridColumn.SortMemberPath + "]" + " = ''") || newFilter.Contains("[" + gridColumn.SortMemberPath + "]" + " IS Null"));
                        if (idexAppliedOn == 1)
                            newFilter = "(" + newFilter + ")";

                        if (!isEnumType)
                        {
                            newFilter = newFilter + " AND " + "(" + "[" + columnName + "]" + " " + " IN " + "(" + "'" + value +
                                  "'" + ")" + ")";
                        }
                        else
                        {
                            newFilter = newFilter + " AND " + "(" + "[" + columnName + "]" + " " + " IN " + "(" + enumValue +
                                   ")" + ")";
                        }
                    }
                    else
                    {
                        int startIndex = newFilter.IndexOf("[" + columnName + "]" + " " + " IN ", StringComparison.Ordinal);
                        newFilter = FilterNullAndBlanlValues(value, columnName, newFilter, startIndex);
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(value)))
                    {
                        if (!isEnumType)
                            newFilter = "[" + columnName + "]" + " " + " IN " + "(" + "'" + value + "'" + ")";
                        else
                        {
                            newFilter = "[" + columnName + "]" + " " + " IN " + "(" + value + ")";
                        }
                    }
                    else
                    {
                        int startIndex = newFilter.IndexOf("[" + columnName + "]" + " " + " IN ", StringComparison.Ordinal);
                        newFilter = FilterNullAndBlanlValues(value, columnName, newFilter, startIndex);
                    }
                }
            }


            return newFilter.Trim();
        }

        private List<string> GetFilteredColumnValues(string columnName, DataView view)
        {
            string newFilter = view.RowFilter;
            if (newFilter.Contains("[" + columnName + "]" + " " + " IN "))
            {
                int startIndex = newFilter.IndexOf("[" + columnName + "]" + " " + " IN ", StringComparison.Ordinal);
                string actaulFilter =
                    newFilter.Substring(startIndex + ("[" + columnName + "]" + " " + " IN ").Length + 1);
                int lastIndex = actaulFilter.IndexOf(")", StringComparison.Ordinal);
                actaulFilter = actaulFilter.Substring(0, lastIndex);
                string[] listOfFilter = actaulFilter.Split(',');
                List<string> list = listOfFilter.ToList();
                var newList = new List<String>();
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].IndexOf("'", StringComparison.Ordinal) == 0 && list[i].LastIndexOf("'", StringComparison.Ordinal) == list[i].Length - 1)
                    {
                        newList.Add(list[i]);
                        continue;
                    }
                    string newfilterYoBeAdded = list[i];

                    for (int j = i + 1; j < list.Count; j++)
                    {
                        i = j;
                        string current = list[j];
                        if (current.LastIndexOf("'", StringComparison.Ordinal) == current.Length - 1)
                        {
                            if (current.Length > 1 && current[current.Length - 2].ToString(CultureInfo.InvariantCulture) != "'")
                            {
                                newfilterYoBeAdded = newfilterYoBeAdded + "," + current;
                                newList.Add(newfilterYoBeAdded);
                                break;
                            }
                        }
                        else
                        {
                            newfilterYoBeAdded = newfilterYoBeAdded + "," + current;
                        }
                    }
                }
                if (newFilter.Contains("([" + columnName + "] IS Null)"))
                {
                    newList.Add(null);
                }
                if (newFilter.Contains("([" + columnName + "] = '')"))
                {
                    newList.Add("");
                }

                return newList;
            }

            return new List<string>();
        }

        private List<string> GetFilteredColumnValues(string columnName, string rowFilter)
        {
            string newFilter = rowFilter;
            List<String> newList = new List<String>();
            if (newFilter.Contains("[" + columnName + "]" + " " + " IN "))
            {
                int startIndex = newFilter.IndexOf("[" + columnName + "]" + " " + " IN ", StringComparison.Ordinal);
                string actaulFilter =
                    newFilter.Substring(startIndex + ("[" + columnName + "]" + " " + " IN ").Length + 1);
                int lastIndex = actaulFilter.IndexOf(")", StringComparison.Ordinal);
                actaulFilter = actaulFilter.Substring(0, lastIndex);
                string[] listOfFilter = actaulFilter.Split(',');
                List<string> list = listOfFilter.ToList();

                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].IndexOf("'", StringComparison.Ordinal) == 0 && list[i].LastIndexOf("'", StringComparison.Ordinal) == list[i].Length - 1)
                    {
                        newList.Add(list[i]);
                        continue;
                    }
                    string newfilterYoBeAdded = list[i];

                    for (int j = i + 1; j < list.Count; j++)
                    {
                        i = j;
                        string current = list[j];
                        if (current.LastIndexOf("'", StringComparison.Ordinal) == current.Length - 1)
                        {
                            if (current.Length > 1 && current[current.Length - 2].ToString(CultureInfo.InvariantCulture) != "'")
                            {
                                newfilterYoBeAdded = newfilterYoBeAdded + "," + current;
                                newList.Add(newfilterYoBeAdded);
                                break;
                            }
                        }
                        else
                        {
                            newfilterYoBeAdded = newfilterYoBeAdded + "," + current;
                        }
                    }
                }
                if (newFilter.Contains("([" + columnName + "] IS Null)"))
                {
                    newList.Add(null);
                }
                if (newFilter.Contains("([" + columnName + "] = '')"))
                {
                    newList.Add("");
                }

            }
            else if (columnNameHasIndexer(columnName))
            {
                string collProp = getCollectionNameOfIndexer(columnName);
                string collKey = getKeyOfIndexer(columnName);
                var key = new Tuple<string, string>(collProp, collKey);

                if (Filter.IndexedColumnFilter.ContainsKey(key))
                {
                    newList = Filter.IndexedColumnFilter[key].ToList();
                }
            }

            return newList;
        }

        public void RemoveAllFilter(DataGrid currentGrid, string columnName)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("RemoveAllFilter col:{0}", columnName));

            if (currentGrid.ItemsSource == null) return;
            string newFilter = null;
            if (currentGrid.ItemsSource is DataView)
            {
                var itemSource = (DataView)currentGrid.ItemsSource;
                newFilter = itemSource.RowFilter;
            }
            else if (CollectionViewSource.GetDefaultView(currentGrid.ItemsSource) != null)
            {
                newFilter = ((ExtendedDataGrid)currentGrid).AutoFilterHelper.FilterExpression;
            }

            unregisterFilteredColumns(columnName);


            if (columnNameHasIndexer(columnName))
            {
                string collProp = getCollectionNameOfIndexer(columnName);
                string collKey = getKeyOfIndexer(columnName);
                var key = new Tuple<string, string>(collProp, collKey);

                if (Filter.IndexedColumnFilter.ContainsKey(key))
                {
                    Filter.IndexedColumnFilter.Remove(key);
                }
            }
            if (!string.IsNullOrEmpty(newFilter))
            {

                if (newFilter.Contains("[" + columnName + "]" + " " + " IN "))
                {
                    int startIndex = newFilter.IndexOf("[" + columnName + "]" + " " + " IN ", StringComparison.Ordinal);
                    string actaulFilter =
                        newFilter.Substring(startIndex + ("[" + columnName + "]" + " " + " IN ").Length + 1);
                    int lastIndex = actaulFilter.IndexOf(")", StringComparison.Ordinal);
                    actaulFilter = actaulFilter.Substring(0, lastIndex);
                    string relalValue = "[" + columnName + "]" + " " + " IN " + "(" + actaulFilter + ")";
                    if (newFilter.Contains("("  + relalValue +  ")"))
                    {
                        newFilter = newFilter.Replace("(" + relalValue + ")", "");
                        if (newFilter.IndexOf("( AND (", StringComparison.Ordinal) == 0)
                        {
                            newFilter = newFilter.Replace("( AND (", "");
                        }
                        if (newFilter.IndexOf("(((", StringComparison.Ordinal) == 0)
                        {
                            newFilter = newFilter.Substring(2);
                        }
                        if (newFilter.Contains("))) AND "))
                        {
                            newFilter = newFilter.Replace("))) AND ", ")");
                        }

                        if (newFilter.LastIndexOf(" AND", StringComparison.Ordinal) == newFilter.Length - 5)
                        {
                            newFilter = newFilter.Substring(0, newFilter.Length - 5);
                        }
                        if (newFilter.Contains(" AND )"))
                        {
                            newFilter = newFilter.Replace(" AND )", "");
                        }
                        if (newFilter.IndexOf("((", StringComparison.Ordinal) == 0)
                        {
                            newFilter = newFilter.Substring(2);
                        }
                        if (newFilter.IndexOf(" AND ", StringComparison.Ordinal) == 0)
                        {
                            newFilter = newFilter.Substring(5);
                        }
                    }
                    else
                    {
                        newFilter = newFilter.Replace(relalValue, "");
                        if (newFilter == "')")
                        {
                            newFilter = "";
                        }

                        if (currentGrid.ItemsSource is DataView)
                        {
                            var itemSource = (DataView)currentGrid.ItemsSource;
                            itemSource.RowFilter = CorrectRowFilter(newFilter);
                        }
                        else if (CollectionViewSource.GetDefaultView(currentGrid.ItemsSource) != null)
                        {
                            ((ExtendedDataGrid)currentGrid).AutoFilterHelper.FilterExpression = CorrectRowFilter(newFilter);
                        }
                        var stackPanel = CurrentListBox.Parent as StackPanel;
                        if (stackPanel != null)
                        {
                            var popup = stackPanel.Parent as Popup;
                            if (popup != null)
                            {
                                popup.Tag = "True";
                            }
                        }

                        return;
                    }
                }
                newFilter = RemoveIsNullAndBlankFilter(newFilter, columnName);
                if (string.IsNullOrEmpty(newFilter) && FilteredColumns.Any())
                {
                    newFilter = "True";
                }

                if (currentGrid.ItemsSource is DataView)
                {
                    var itemSource = (DataView)currentGrid.ItemsSource;
                    itemSource.RowFilter = CorrectRowFilter(newFilter);
                }
                else if (CollectionViewSource.GetDefaultView(currentGrid.ItemsSource) != null)
                {
                    ((ExtendedDataGrid)currentGrid).AutoFilterHelper.FilterExpression = CorrectRowFilter(newFilter);
                }
                int count = CurrentDistictValues.Count(c => c.IsChecked);
                if (count == CurrentDistictValues.Count - 1)
                {
                    CurrentDistictValues[0].IsChecked = true;
                    if (CurrentListBox != null)
                    {
                        CurrentListBox.ItemsSource = CurrentDistictValues;
                        CurrentListBox.UpdateLayout();
                        CurrentListBox.Items.Refresh();
                    }
                }
                if (CurrentListBox != null)
                {
                    var clearButton = FindControls.FindChild<Button>(CurrentListBox.Parent, "btnClear");
                    if (clearButton != null)
                    {
                        clearButton.IsEnabled = CurrentDistictValues.Count(c => c.IsChecked) > 0;
                    }
                }
            }

            CurrentGrid.Items.Refresh();
        }

        public void RemoveFilters(DataGrid grid, string columnName, object value)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("RemoveFilters col:{0} val:{1}", columnName, value));
            var isEnumType = false;
            int enumValue = -1;
            if (CurrentGrid.ItemsSource is DataView)
            {
                var dv = CurrentGrid.ItemsSource as DataView;
                var baseType = dv.Table.Columns[columnName].DataType.BaseType;
                if (baseType != null)
                {
                    isEnumType = baseType.ToString().Equals("System.Enum");
                    if (isEnumType)
                        enumValue = Convert.ToInt32(Enum.Parse(dv.Table.Columns[columnName].DataType, Convert.ToString(value)));
                }
            }
            else if (CollectionViewSource.GetDefaultView(CurrentGrid.ItemsSource) != null)
            {
                var readOnlyCollection = (((ListCollectionView)(CollectionViewSource.GetDefaultView(CurrentGrid.ItemsSource)))).ItemProperties;
                if (readOnlyCollection != null)
                {
                    if (columnNameHasIndexer(columnName))
                    {
                        string collProp = getCollectionNameOfIndexer(columnName);
                        string collKey = getKeyOfIndexer(columnName);
                        var key = new Tuple<string, string>(collProp, collKey);

                        if (Filter.IndexedColumnFilter.ContainsKey(key))
                        {
                            Filter.IndexedColumnFilter[key].Remove(Convert.ToString(value));
                            if (Filter.IndexedColumnFilter[key].Count == 0)
                            {
                                Filter.IndexedColumnFilter.Remove(key);
                            }
                        }
                    }
                    else
                    {
                        var item =
                            readOnlyCollection.First(
                                c => c.Name == columnName);
                        if (item.PropertyType.BaseType != null && item.PropertyType.BaseType.ToString() == "System.Enum")
                        {
                            isEnumType = true;
                            enumValue = Convert.ToInt32(Enum.Parse(item.PropertyType, Convert.ToString(value)));
                        }
                    }
                }
            }
            value = AddEscapeSequece(value);
            string rowFilter = null;
            if (grid.ItemsSource == null)
                return;
            if (grid.ItemsSource is DataView)
            {
                rowFilter = ((DataView)grid.ItemsSource).RowFilter;
            }
            else if (CollectionViewSource.GetDefaultView(grid.ItemsSource) != null)
            {
                rowFilter = ((ExtendedDataGrid)grid).AutoFilterHelper.FilterExpression;
            }

            if (!string.IsNullOrEmpty(rowFilter))
            {
                string newFilter = rowFilter;
                if (newFilter.Contains("[" + columnName + "]" + " " + " IN ") &&
                    !string.IsNullOrEmpty(Convert.ToString(value)))
                {
                    int startIndex = newFilter.IndexOf("[" + columnName + "]" + " " + " IN ", StringComparison.Ordinal);
                    string actaulFilter =
                        newFilter.Substring(startIndex + ("[" + columnName + "]" + " " + " IN ").Length + 1);
                    int lastIndex = actaulFilter.LastIndexOf(")", StringComparison.Ordinal);
                    actaulFilter = actaulFilter.Substring(0, lastIndex);
                    string[] listOfFilter = actaulFilter.Split(',');
                    if (!isEnumType)
                    {
                        if (listOfFilter.Contains("'" + value + "'"))
                        {
                            string realFilter = "[" + columnName + "]" + " " + " IN " + "(" + actaulFilter + ")";
                            string replaced = realFilter.Replace("'" + value + "'", "");
                            if (replaced.Contains(",,"))
                            {
                                replaced = replaced.Replace(",,", ",");
                            }
                            if (replaced.Contains(",)"))
                            {
                                replaced = replaced.Replace(",)", ")");
                            }
                            if (replaced.Contains("()"))
                            {
                                replaced = "";
                            }
                            if (replaced.Contains("(,"))
                            {
                                replaced = replaced.Replace("(,", "(");
                            }
                            if (replaced.Contains(",)"))
                            {
                                replaced = replaced.Replace(",)", ")");
                            }
                            if (newFilter.Contains(" AND ()"))
                            {
                                newFilter = newFilter.Replace(" AND ()", "");
                            }
                            newFilter = newFilter.Replace(realFilter, replaced);
                            if (newFilter.Contains("() AND "))
                            {
                                newFilter = newFilter.Replace("() AND ", "");
                            }
                            if (newFilter.IndexOf("(((", StringComparison.Ordinal) == 0)
                            {
                                newFilter = newFilter.Substring(2);
                            }
                            if (newFilter.Contains(")))"))
                            {
                                newFilter = newFilter.Replace(")))", "))");
                            }
                            if (newFilter.Contains("()"))
                            {
                                newFilter = newFilter.Replace("()", "");
                            }
                            if (newFilter.LastIndexOf(" AND ", StringComparison.Ordinal) == newFilter.Length - 5)
                            {
                                newFilter = newFilter.Substring(0, newFilter.Length - 5);
                            }
                            if (newFilter.IndexOf("((", StringComparison.Ordinal) == 0)
                            {
                                newFilter = newFilter.Substring(1);
                            }
                            newFilter = newFilter.Trim();
                            if (newFilter.IndexOf("Or", StringComparison.Ordinal) == 0)
                            {
                                newFilter = newFilter.Substring(3);
                            }

                            switch (newFilter)
                            {
                                case "":
                                    if (grid.ItemsSource is DataView)
                                    {
                                        ((DataView)grid.ItemsSource).RowFilter = null;
                                    }
                                    else if (CollectionViewSource.GetDefaultView(grid.ItemsSource) != null)
                                    {
                                        ((ExtendedDataGrid)grid).AutoFilterHelper.FilterExpression = null;
                                    }
                                    return;
                                default:
                                    if (grid.ItemsSource is DataView)
                                    {
                                        ((DataView)grid.ItemsSource).RowFilter = CorrectRowFilter(newFilter);
                                    }
                                    else if (CollectionViewSource.GetDefaultView(grid.ItemsSource) != null)
                                    {
                                        ((ExtendedDataGrid)grid).AutoFilterHelper.FilterExpression = CorrectRowFilter(newFilter);
                                    }
                                    break;
                            }

                            int count1 =
                                CurrentDistictValues.Count(c => c.IsChecked && Convert.ToString(c.Name) != "(Select All)");
                            if (count1 == CurrentDistictValues.Count - 1)
                            {
                                CurrentDistictValues[0].IsChecked = true;
                                if (CurrentListBox != null)
                                {
                                    CurrentListBox.ItemsSource = CurrentDistictValues;
                                    CurrentListBox.UpdateLayout();
                                    CurrentListBox.Items.Refresh();
                                }
                            }
                            else
                            {
                                CurrentDistictValues[0].IsChecked = false;
                                if (CurrentListBox != null)
                                {
                                    CurrentListBox.ItemsSource = CurrentDistictValues;
                                    CurrentListBox.UpdateLayout();
                                    CurrentListBox.Items.Refresh();
                                }
                            }
                        }
                    }
                    else
                    {
                        if (listOfFilter.Contains(enumValue.ToString(CultureInfo.InvariantCulture)))
                        {
                            string realFilter = "[" + columnName + "]" + " " + " IN " + "(" + actaulFilter + ")";
                            string replaced = realFilter.Replace(enumValue.ToString(CultureInfo.InvariantCulture), "");
                            if (replaced.Contains(",,"))
                            {
                                replaced = replaced.Replace(",,", ",");
                            }
                            if (replaced.Contains(",)"))
                            {
                                replaced = replaced.Replace(",)", ")");
                            }
                            if (replaced.Contains("()"))
                            {
                                replaced = "";
                            }
                            if (replaced.Contains("(,"))
                            {
                                replaced = replaced.Replace("(,", "(");
                            }
                            if (replaced.Contains(",)"))
                            {
                                replaced = replaced.Replace(",)", ")");
                            }
                            if (newFilter.Contains(" AND ()"))
                            {
                                newFilter = newFilter.Replace(" AND ()", "");
                            }
                            newFilter = newFilter.Replace(realFilter, replaced);
                            if (newFilter.Contains("() AND "))
                            {
                                newFilter = newFilter.Replace("() AND ", "");
                            }
                            if (newFilter.IndexOf("(((", StringComparison.Ordinal) == 0)
                            {
                                newFilter = newFilter.Substring(2);
                            }
                            if (newFilter.Contains(")))"))
                            {
                                newFilter = newFilter.Replace(")))", "))");
                            }
                            if (newFilter.Contains("()"))
                            {
                                newFilter = newFilter.Replace("()", "");
                            }
                            if (newFilter.LastIndexOf(" AND ", StringComparison.Ordinal) == newFilter.Length - 5)
                            {
                                newFilter = newFilter.Substring(0, newFilter.Length - 5);
                            }
                            if (newFilter.IndexOf("((", StringComparison.Ordinal) == 0)
                            {
                                newFilter = newFilter.Substring(1);
                            }
                            newFilter = newFilter.Trim();
                            if (newFilter.IndexOf("Or", StringComparison.Ordinal) == 0)
                            {
                                newFilter = newFilter.Substring(3);
                            }

                            switch (newFilter)
                            {
                                case "":
                                    if (grid.ItemsSource is DataView)
                                    {
                                        ((DataView)grid.ItemsSource).RowFilter = null;
                                    }
                                    else if (CollectionViewSource.GetDefaultView(grid.ItemsSource) != null)
                                    {
                                        ((ExtendedDataGrid)grid).AutoFilterHelper.FilterExpression = null;
                                    }
                                    return;
                                default:
                                    if (grid.ItemsSource is DataView)
                                    {
                                        ((DataView)grid.ItemsSource).RowFilter = CorrectRowFilter(newFilter);
                                    }
                                    else if (CollectionViewSource.GetDefaultView(grid.ItemsSource) != null)
                                    {
                                        ((ExtendedDataGrid)grid).AutoFilterHelper.FilterExpression = CorrectRowFilter(newFilter);
                                    }
                                    break;
                            }

                            int count1 =
                                CurrentDistictValues.Count(c => c.IsChecked && Convert.ToString(c.Name) != "(Select All)");
                            if (count1 == CurrentDistictValues.Count - 1)
                            {
                                CurrentDistictValues[0].IsChecked = true;
                                if (CurrentListBox != null)
                                {
                                    CurrentListBox.ItemsSource = CurrentDistictValues;
                                    CurrentListBox.UpdateLayout();
                                    CurrentListBox.Items.Refresh();
                                }
                            }
                            else
                            {
                                CurrentDistictValues[0].IsChecked = false;
                                if (CurrentListBox != null)
                                {
                                    CurrentListBox.ItemsSource = CurrentDistictValues;
                                    CurrentListBox.UpdateLayout();
                                    CurrentListBox.Items.Refresh();
                                }
                            }
                        }
                    }

                }
                else
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(value)))
                    {
                        if (grid.ItemsSource is DataView)
                        {
                            string actualValue = "";
                            var itemSource = grid.ItemsSource as DataView;
                            var unquieValues = new HashSet<string>();
                            foreach (DataRow row in itemSource.Table.Rows)
                            {
                                var val = Convert.ToString(row[columnName]);
                                if (val == Convert.ToString(value))
                                    continue;
                                if (!unquieValues.Contains(val))
                                    unquieValues.Add(val);
                                else
                                    continue;
                                if (actualValue == "")
                                {
                                    actualValue = val + "'";
                                }
                                else
                                {
                                    actualValue = actualValue + "," + "'" + val + "'";
                                }
                            }
                            actualValue = actualValue.Substring(0, actualValue.Length - 1);
                            ApplyFilters(grid, columnName, actualValue);
                        }
                        else if (CollectionViewSource.GetDefaultView(grid.ItemsSource) != null)
                        {
                            ICollectionView view = CollectionViewSource.GetDefaultView(grid.ItemsSource);
                            var unquieValues = new HashSet<string>();
                            string actualValue = "";
                            foreach (var rowData in grid.ItemsSource)
                            {
                                string data = null;
                                if (columnNameHasIndexer(columnName))
                                {
                                    data = getIndexerColumnValue(columnName, rowData);
                                }
                                else
                                {
                                    var propertyValue = rowData.GetType().GetProperty(columnName);
                                    if (propertyValue != null)
                                    {
                                        data = Convert.ToString(propertyValue.GetValue(rowData, null));
                                    }
                                }
                                if (!unquieValues.Contains(data) && data != Convert.ToString(value))
                                {
                                    if (actualValue == "")
                                    {
                                        actualValue = data + "'";
                                    }
                                    else
                                    {
                                        actualValue = actualValue + "," + "'" + data + "'";
                                    }
                                    unquieValues.Add(data);
                                }

                            }
                            actualValue = actualValue.Substring(0, actualValue.Length - 1);
                            ApplyFilters(grid, columnName, actualValue);
                        }


                    }
                    else
                    {
                        if (value == null)
                        {
                            if (grid.ItemsSource is DataView)
                            {
                                var itemSource = grid.ItemsSource as DataView;
                                if (newFilter.Contains("Or ([" + columnName + "] IS Null)"))
                                {
                                    newFilter = newFilter.Replace("Or ([" + columnName + "] IS Null)", "");
                                    itemSource.RowFilter = CorrectRowFilter(newFilter);
                                }
                                else if (newFilter.Contains("([" + columnName + "] IS Null)"))
                                {
                                    newFilter = newFilter.Replace("([" + columnName + "] IS Null)", "");
                                    itemSource.RowFilter = CorrectRowFilter(newFilter);
                                }
                                else if (newFilter.Contains("Or [" + columnName + "] IS Null"))
                                {
                                    newFilter = newFilter.Replace("Or [" + columnName + "] IS Null", "");
                                    itemSource.RowFilter = CorrectRowFilter(newFilter);
                                }
                                else if (newFilter.Contains("[" + columnName + "] IS Null"))
                                {
                                    newFilter = newFilter.Replace("[" + columnName + "] IS Null", "");
                                    itemSource.RowFilter = CorrectRowFilter(newFilter);
                                }
                            }
                            else if (CollectionViewSource.GetDefaultView(grid.ItemsSource) != null)
                            {
                                var mainGrid = grid as ExtendedDataGrid;
                                if (newFilter.Contains("Or ([" + columnName + "] IS Null)"))
                                {
                                    newFilter = newFilter.Replace("Or ([" + columnName + "] IS Null)", "");
                                    mainGrid.AutoFilterHelper.FilterExpression = CorrectRowFilter(newFilter);
                                }
                                else if (newFilter.Contains("([" + columnName + "] IS Null)"))
                                {
                                    newFilter = newFilter.Replace("([" + columnName + "] IS Null)", "");
                                    mainGrid.AutoFilterHelper.FilterExpression = CorrectRowFilter(newFilter);
                                }
                                else if (newFilter.Contains("Or [" + columnName + "] IS Null"))
                                {
                                    newFilter = newFilter.Replace("Or [" + columnName + "] IS Null", "");
                                    mainGrid.AutoFilterHelper.FilterExpression = CorrectRowFilter(newFilter);
                                }
                                else if (newFilter.Contains("[" + columnName + "] IS Null"))
                                {
                                    newFilter = newFilter.Replace("[" + columnName + "] IS Null", "");
                                    mainGrid.AutoFilterHelper.FilterExpression = CorrectRowFilter(newFilter);
                                }
                            }

                        }
                        else
                        {
                            if (grid.ItemsSource is DataView)
                            {
                                var itemSource = grid.ItemsSource as DataView;
                                if (newFilter.Contains("Or ([" + columnName + "] = '')"))
                                {
                                    newFilter = newFilter.Replace("Or ([" + columnName + "] = '')", "");

                                    itemSource.RowFilter = CorrectRowFilter(newFilter);
                                }
                                else if (newFilter.Contains("([" + columnName + "] = '')"))
                                {
                                    newFilter = newFilter.Replace("([" + columnName + "] = '')", "");
                                    itemSource.RowFilter = CorrectRowFilter(newFilter);
                                }
                                else if (newFilter.Contains("Or [" + columnName + "] = ''"))
                                {
                                    newFilter = newFilter.Replace("Or [" + columnName + "] = ''", "");
                                    itemSource.RowFilter = CorrectRowFilter(newFilter);
                                }
                                else if (newFilter.Contains("[" + columnName + "] = ''"))
                                {
                                    newFilter = newFilter.Replace("[" + columnName + "] = ''", "");
                                    itemSource.RowFilter = CorrectRowFilter(newFilter);
                                }
                            }
                            else if (CollectionViewSource.GetDefaultView(grid.ItemsSource) != null)
                            {
                                var mainGrid = grid as ExtendedDataGrid;
                                if (newFilter.Contains("Or ([" + columnName + "] = '')"))
                                {
                                    newFilter = newFilter.Replace("Or ([" + columnName + "] = '')", "");

                                    mainGrid.AutoFilterHelper.FilterExpression = CorrectRowFilter(newFilter);
                                }
                                else if (newFilter.Contains("([" + columnName + "] = '')"))
                                {
                                    newFilter = newFilter.Replace("([" + columnName + "] = '')", "");
                                    mainGrid.AutoFilterHelper.FilterExpression = CorrectRowFilter(newFilter);
                                }
                                else if (newFilter.Contains("Or [" + columnName + "] = ''"))
                                {
                                    newFilter = newFilter.Replace("Or [" + columnName + "] = ''", "");
                                    mainGrid.AutoFilterHelper.FilterExpression = CorrectRowFilter(newFilter);
                                }
                                else if (newFilter.Contains("[" + columnName + "] = ''"))
                                {
                                    newFilter = newFilter.Replace("[" + columnName + "] = ''", "");
                                    mainGrid.AutoFilterHelper.FilterExpression = CorrectRowFilter(newFilter);
                                }
                            }


                        }
                    }
                    CurrentDistictValues[0].IsChecked = false;
                    CurrentListBox.Items.Refresh();
                    grid.Items.Refresh();
                }
            }
            else
            {
                if (grid.ItemsSource is DataView)
                {
                    var itemSource = grid.ItemsSource as DataView;
                    string actualValue = "";
                    var unquieValues = new HashSet<string>();
                    foreach (DataRow row in itemSource.Table.Rows)
                    {
                        var val = Convert.ToString(row[columnName]);
                        if (val == Convert.ToString(value))
                            continue;
                        if (!unquieValues.Contains(val))
                            unquieValues.Add(val);
                        else
                            continue;
                        if (actualValue == "")
                        {
                            actualValue = val + "'";
                        }
                        else
                        {
                            actualValue = actualValue + "," + "'" + val + "'";
                        }
                    }
                    actualValue = actualValue.Substring(0, actualValue.Length - 1);
                    ApplyFilters(grid, columnName, actualValue);
                    CurrentDistictValues[0].IsChecked = false;
                    grid.Items.Refresh();
                }
                else if (CollectionViewSource.GetDefaultView(grid.ItemsSource) != null)
                {
                    ICollectionView view = CollectionViewSource.GetDefaultView(grid.ItemsSource);
                    var unquieValues = new HashSet<string>();
                    string actualValue = "";
                    foreach (var rowData in grid.ItemsSource)
                    {
                        string data = null;
                        if (columnNameHasIndexer(columnName))
                        {
                            data = getIndexerColumnValue(columnName, rowData);
                        }
                        else
                        {
                            var propertyValue = rowData.GetType().GetProperty(columnName);
                            if (propertyValue != null)
                            {
                                data = Convert.ToString(propertyValue.GetValue(rowData, null));
                            }
                        }

                        if (!unquieValues.Contains(data) && data != Convert.ToString(value))
                        {
                            if (actualValue == "")
                            {
                                actualValue = data + "'";
                            }
                            else
                            {
                                actualValue = actualValue + "," + "'" + data + "'";
                            }
                            unquieValues.Add(data);
                        }
                    }
                    actualValue = actualValue.Substring(0, actualValue.Length - 1);
                    ApplyFilters(grid, columnName, actualValue);
                    CurrentDistictValues[0].IsChecked = false;
                    grid.Items.Refresh();
                }

            }
            if (CurrentListBox != null)
            {
                if (CurrentListBox != null)
                {
                    var clearButton = FindControls.FindChild<Button>(CurrentListBox.Parent, "btnClear");
                    if (clearButton != null)
                    {
                        if (CurrentDistictValues != null)
                            clearButton.IsEnabled = CurrentDistictValues.Count(c => c.IsChecked) > 0;

                    }
                }
            }

            grid.Items.Refresh();
        }



        private string RemoveIsNullAndBlankFilter(string existingFilter, string columnName)
        {
            string newFilter = existingFilter.Trim();

            //IS NULL Values

            if (newFilter.Contains("Or ([" + columnName + "] IS Null)"))
            {
                newFilter = newFilter.Replace("Or ([" + columnName + "] IS Null)", "");
                newFilter = CorrectRowFilter(newFilter);
            }
            else if (newFilter.Contains("([" + columnName + "] IS Null)"))
            {
                newFilter = newFilter.Replace("([" + columnName + "] IS Null)", "");
                newFilter = CorrectRowFilter(newFilter);
            }
            else if (newFilter.Contains("Or [" + columnName + "] IS Null"))
            {
                newFilter = newFilter.Replace("Or [" + columnName + "] IS Null", "");
                newFilter = CorrectRowFilter(newFilter);
            }
            else if (newFilter.Contains("[" + columnName + "] IS Null"))
            {
                newFilter = newFilter.Replace("[" + columnName + "] IS Null", "");
                newFilter = CorrectRowFilter(newFilter);
            }

            //BLANK values

            if (newFilter == null)
                return null;

            if (newFilter.Contains("Or ([" + columnName + "] = '')"))
            {
                newFilter = newFilter.Replace("Or ([" + columnName + "] = '')", "");

                newFilter = CorrectRowFilter(newFilter);
            }
            else if (newFilter.Contains("([" + columnName + "] = '')"))
            {
                newFilter = newFilter.Replace("([" + columnName + "] = '')", "");
                newFilter = CorrectRowFilter(newFilter);
            }
            else if (newFilter.Contains("Or [" + columnName + "] = ''"))
            {
                newFilter = newFilter.Replace("Or [" + columnName + "] = ''", "");
                newFilter = CorrectRowFilter(newFilter);
            }
            else if (newFilter.Contains("[" + columnName + "] = ''"))
            {
                newFilter = newFilter.Replace("[" + columnName + "] = ''", "");
                newFilter = CorrectRowFilter(newFilter);
            }
            else if (newFilter.IndexOf("() AND ", StringComparison.Ordinal) == 0)
            {
                newFilter = newFilter.Substring(7);
                newFilter = CorrectRowFilter(newFilter);
            }

            return newFilter;
        }

        private string CorrectRowFilter(string exsistingFilter)
        {
            string newFilter = Convert.ToString(exsistingFilter);

            if (string.IsNullOrEmpty(newFilter))
                return null;

            newFilter = exsistingFilter.Trim();

            if (string.IsNullOrEmpty(newFilter))
                return null;

            if (newFilter.Contains(",)"))
            {
                newFilter = newFilter.Replace(",)", ")");
            }
            if (newFilter.Contains("()"))
            {
                newFilter = "";
            }
            if (newFilter.Contains("(,"))
            {
                newFilter = newFilter.Replace("(,", "(");
            }
            if (newFilter.Contains(",)"))
            {
                newFilter = newFilter.Replace(",)", ")");
            }
            if (newFilter.Contains(" AND ()"))
            {
                newFilter = newFilter.Replace(" AND ()", "");
            }
            newFilter = newFilter.Replace(exsistingFilter, newFilter);
            if (newFilter.Contains("() AND "))
            {
                newFilter = newFilter.Replace("() AND ", "");
            }
            if (newFilter.IndexOf("(((", StringComparison.Ordinal) == 0)
            {
                newFilter = newFilter.Substring(2);
            }
            if (newFilter.Contains(")))"))
            {
                newFilter = newFilter.Replace(")))", "))");
            }
            if (newFilter.Contains("()"))
            {
                newFilter = newFilter.Replace("()", "");
            }
            if (newFilter.LastIndexOf(" AND ", StringComparison.Ordinal) == newFilter.Length - 5 && newFilter.Length - 5 != -1)
            {
                newFilter = newFilter.Substring(0, newFilter.Length - 5);
            }
            if (newFilter.IndexOf("((", StringComparison.Ordinal) == 0)
            {
                newFilter = newFilter.Substring(1);
            }
            newFilter = newFilter.Trim();
            if (newFilter.IndexOf("Or", StringComparison.Ordinal) == 0)
            {
                newFilter = newFilter.Substring(3);
            }

            if (newFilter.LastIndexOf(" )", StringComparison.Ordinal) == newFilter.Length - 2 && newFilter.Length - 2 > 0)
            {
                newFilter = newFilter.Substring(0, newFilter.Length - 2) + newFilter.Substring(newFilter.Length - 1);
            }
            if (newFilter.LastIndexOf("AND", StringComparison.Ordinal) == newFilter.Length - 3)
            {
                newFilter = newFilter.Substring(0, newFilter.Length - 3);
            }

            if (newFilter == "( )")
                newFilter = null;

            switch (newFilter)
            {
                case "":
                    return null;
                default:
                    return newFilter;
            }
        }

        #region ICollectionView
        private FilterParam m_Filter = new FilterParam();
        public FilterParam Filter
        {
            get { return m_Filter; }
            set
            {
                m_Filter = value;
                refreshFilter_helper();
                refreshFilter();
            }
        }

        private void refreshFilter_helper()
        {
            var unFilteredColumn = CurrentGrid.Columns.First(col => !(columnNameHasIndexer(col.SortMemberPath) || FilteredColumns.Contains(col.SortMemberPath)));
            if (unFilteredColumn != null)
            {
                RemoveAllFilter(CurrentGrid, unFilteredColumn.SortMemberPath);
            }

            if (CollectionViewSource.GetDefaultView(CurrentGrid.ItemsSource) != null)
            {
                _view = CollectionViewSource.GetDefaultView(CurrentGrid.ItemsSource);
            }
        }




        public List<string> FilteredColumns
        {
            get { return Filter.FilteredColumns; }
        }


        System.ComponentModel.ICollectionView _view;
        DataTable _collectionViewDt;
        // ** object model
        internal string FilterExpression
        {
            get { return Filter.FilterExpression; }
            set
            {
                Filter.FilterExpression = value;
                refreshFilter();
            }
        }
        private void refreshFilter()
        {
            UpdateFilter();
            _view.Filter = null;
            if (!string.IsNullOrEmpty(FilterExpression))
            {
                _view.Filter = FilterPredicate;
                CurrentGrid.UpdateLayout();
            }

            CurrentGrid.Items.Refresh();

        }
        // ** implementation
        bool FilterPredicate(object obj)
        {
            if (_collectionViewDt == null)
                return false;
            // populate the row
            var row = _collectionViewDt.Rows[0];
            populateRow(obj, row);

            bool returnValue = false;
            Boolean.TryParse(Convert.ToString(row["_filter"]), out returnValue);
            // compute the expression
            returnValue = returnValue && computeIndexedFilter(row);
            return returnValue;
        }

        private static void populateRow(object obj, DataRow row)
        {
            if (obj.GetType() == typeof(ExpandoObject))
            {
                foreach (var propname in Dynamic.GetMemberNames(obj))
                {
                    object val = Dynamic.InvokeGet(obj, propname);
                    row[propname] = val ?? string.Empty;
                }
            }
            else
            {
                foreach (var pi in obj.GetType().GetProperties())
                {
                    // Bugfix: do not evaluate indexed propertiess
                    if (pi.GetIndexParameters().Any() == false)
                    {
                        if (pi.PropertyType.BaseType != null && pi.PropertyType.BaseType.FullName == "System.Enum")
                        {
                            var value = Convert.ToInt32(System.Enum.Parse(pi.PropertyType, System.Convert.ToString(pi.GetValue(obj, null))));
                            row[pi.Name] = value;
                        }
                        else
                        {
                            try
                            {
                                row[pi.Name] = pi.GetValue(obj, null);
                            }
                            catch (Exception)
                            {
                                row[pi.Name] = DBNull.Value;
                            }
                        }
                    }
                }
            }
        }

        private bool computeIndexedFilter(DataRow row)
        {
            bool res = true;
            foreach (var item in Filter.IndexedColumnFilter)
            {
                string colName = item.Key.Item1;
                string indexerKey = item.Key.Item2;
                IEnumerable<string> values = item.Value;
                IDictionary collection = (IDictionary)row[item.Key.Item1];
                res = res && values.Contains(collection[indexerKey]);


                if (!res)
                {
                    break;
                }
            }

            return res;
        }


        private void registerFilteredColumns(string columnName)
        {
            if (!FilteredColumns.Contains(columnName))
            {
                FilteredColumns.Add(columnName);
            }
        }

        private void unregisterFilteredColumns(string columnName)
        {
            FilteredColumns.Remove(columnName);
        }
        private string getIndexerColumnValue(string columnName, object rowData)
        {
            string collProp = getCollectionNameOfIndexer(columnName);
            string collKey = getKeyOfIndexer(columnName);
            Type t = rowData.GetType();
            var colPropInfo = t.GetProperty(collProp);
            IDictionary collection = (IDictionary)colPropInfo.GetValue(rowData);

            return Convert.ToString(collection[collKey]);
        }

        private string EscapeLikeValue(string value)
        {
            StringBuilder sb = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case ']':
                    case '[':
                    case '%':
                    case '*':

                        sb.Append("[").Append(c).Append("]");
                        break;
                    //case '\'':

                    //    sb.Append("''");
                    //    break;
                    default:
                        sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
        void UpdateFilter()
        {
            _collectionViewDt = null;
            if (_view == null)
            {
                return;
            }
            var enumerator = _view.GetEnumerator();
            if (enumerator.MoveNext()) // sets it to the first element
            {
                var firstElement = enumerator.Current;
                if (firstElement != null && !string.IsNullOrEmpty(FilterExpression))
                {
                    // build/rebuild data table
                    var dt = new DataTable();

                    if (firstElement.GetType() == typeof(ExpandoObject))
                    {
                        foreach (var propname in Dynamic.GetMemberNames(firstElement))
                        {
                            object val = Dynamic.InvokeGet(firstElement, propname);
                            Type type = typeof(string);
                            if (val != null)
                            {
                                type = val.GetType();
                            }
                            dt.Columns.Add(propname, type);
                        }
                    }
                    else
                    {
                        foreach (var pi in firstElement.GetType().GetProperties())
                        {
                            dt.Columns.Add(pi.Name, Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType);
                        }
                    }

                    // add calculated column
                    dt.Columns.Add("_filter", typeof(bool), FilterExpression);

                    // create a single row for evaluating expressions
                    if (dt.Rows.Count == 0)
                    {
                        dt.Rows.Add(dt.NewRow());
                    }

                    // done, save table
                    _collectionViewDt = dt;
                }
            }
        }

        #endregion
    }
}
