using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interactivity;
using ExtendedGrid.ExtendedGridControl;
using System.Windows.Controls;

namespace PlayLogger
{
    public class ExtendedDataGrid_PreserveColumnInfo : Behavior<ExtendedDataGrid>
    {
        private ExtendedDataGrid m_TheDG;
        protected override void OnAttached()
        {
            base.OnAttached();
            m_TheDG = AssociatedObject as ExtendedDataGrid;
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
                typeof(string), typeof(ExtendedDataGrid_PreserveColumnInfo),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ColumnInfoChangedCallback)
            );

        private static object s_updateLock = new object();

        private static void ColumnInfoChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var theBahavior = (ExtendedDataGrid_PreserveColumnInfo)dependencyObject;
            //lock (s_updateLock)
            {
                //if (!theBahavior.updatingColumnInfo)
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
        public string ColumnInfo
        {
            get { return (string)GetValue(ColumnInfoProperty); }
            set { SetValue(ColumnInfoProperty, value); }
        }

        private void UpdateColumnInfo()
        {
            updatingColumnInfo = true;
            ColumnInfo = m_TheDG.GetColumnInformation();
            Filter = m_TheDG.GetAutoFilterInformation();
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
            if (Filter != null)
            {
                m_TheDG.SetAutoFilterInformation(Filter);
            }

            m_TheDG.Items.SortDescriptions.Clear();
            if (ColumnInfo != null)
            {
                m_TheDG.SetColumnInformation(ColumnInfo);
            }
        }


        public object Filter
        {
            get { return (object)GetValue(FilterProperty); }
            set { SetValue(FilterProperty, value); }
        }
        public static DependencyProperty FilterProperty = DependencyProperty.Register("Filter",
            typeof(object), typeof(ExtendedDataGrid_PreserveColumnInfo),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnFilterChanged));


        private static void OnFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // insert your code here
            var theBahavior = (ExtendedDataGrid_PreserveColumnInfo)d;
            theBahavior.ColumnInfoChanged();
        }
    }

}
