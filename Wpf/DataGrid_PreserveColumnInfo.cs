using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interactivity;

namespace PlayLogger
{
    public class DataGrid_PreserveColumnInfo : Behavior<DataGrid>
    {
        private DataGrid m_TheDG;
        protected override void OnAttached()
        {
            base.OnAttached();
            m_TheDG = AssociatedObject as DataGrid;
            doRegisterDGEvents();
        }

        private void doRegisterDGEvents()
        {
            m_TheDG.Initialized += OnInitialized;
            m_TheDG.ColumnReordered += OnColumnReordered;
            m_TheDG.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
        }

        private bool inWidthChange = false;
        private bool updatingColumnInfo = false;
        public static readonly DependencyProperty ColumnInfoProperty = DependencyProperty.Register("ColumnInfo",
                typeof(ObservableCollection<ColumnInfo>), typeof(DataGrid_PreserveColumnInfo),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ColumnInfoChangedCallback)
            );

        private static object s_updateLock = new object();

        private static void ColumnInfoChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var theBahavior = (DataGrid_PreserveColumnInfo)dependencyObject;
            //lock (s_updateLock)
            {
                if (!theBahavior.updatingColumnInfo)
                {
                    theBahavior.ColumnInfoChanged();
                }
            }
        }

        protected void OnInitialized(object i_sender, EventArgs e)
        {
            DataGrid dg = i_sender as DataGrid;
            EventHandler sortDirectionChangedHandler = (sender, x) => UpdateColumnInfo();
            EventHandler widthPropertyChangedHandler = (sender, x) => inWidthChange = true;
            var sortDirectionPropertyDescriptor = DependencyPropertyDescriptor.FromProperty(DataGridColumn.SortDirectionProperty, typeof(DataGridColumn));
            var widthPropertyDescriptor = DependencyPropertyDescriptor.FromProperty(DataGridColumn.WidthProperty, typeof(DataGridColumn));

            Action f_clearHandlers = () =>
            {
                foreach (var column in dg.Columns)
                {
                    sortDirectionPropertyDescriptor.RemoveValueChanged(column, sortDirectionChangedHandler);
                    widthPropertyDescriptor.RemoveValueChanged(column, widthPropertyChangedHandler);
                }
            };
            Action f_addHandlers = () =>
            {
                foreach (var column in dg.Columns)
                {
                    sortDirectionPropertyDescriptor.AddValueChanged(column, sortDirectionChangedHandler);
                    widthPropertyDescriptor.AddValueChanged(column, widthPropertyChangedHandler);
                }
            };



            dg.Columns.CollectionChanged += (sender, x) =>
            {
                f_clearHandlers();
                f_addHandlers();
            };


            dg.Unloaded += (sender, x) =>
            {
                f_clearHandlers();
            };


        }
        public ObservableCollection<ColumnInfo> ColumnInfo
        {
            get { return (ObservableCollection<ColumnInfo>)GetValue(ColumnInfoProperty); }
            set { SetValue(ColumnInfoProperty, value); }
        }
        private void UpdateColumnInfo()
        {
            updatingColumnInfo = true;
            ColumnInfo = new ObservableCollection<ColumnInfo>(m_TheDG.Columns.Select((x) => new ColumnInfo(x)));
            updatingColumnInfo = false;
        }
        protected void OnColumnReordered(object i_sender, DataGridColumnEventArgs e)
        {
            UpdateColumnInfo();
        }
        protected void OnPreviewMouseLeftButtonUp(object i_sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (inWidthChange)
            {
                inWidthChange = false;
                UpdateColumnInfo();
            }
        }
        private void ColumnInfoChanged()
        {
            m_TheDG.Items.SortDescriptions.Clear();
            if (ColumnInfo != null)
            {
                foreach (var column in ColumnInfo)
                {
                    var realColumn = m_TheDG.Columns.Where((x) => column.Header.Equals(x.Header)).FirstOrDefault();
                    if (realColumn == null) { continue; }
                    column.Apply(realColumn, m_TheDG.Columns.Count, m_TheDG.Items.SortDescriptions);
                }
            }
        }
    }

}
