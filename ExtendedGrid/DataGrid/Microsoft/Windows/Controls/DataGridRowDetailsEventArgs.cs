//---------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All rights reserved.
//
//---------------------------------------------------------------------------

using System;
using System.Windows;

namespace ExtendedGrid.Microsoft.Windows.Controls
{
    public class DataGridRowDetailsEventArgs : EventArgs
    {
        public DataGridRowDetailsEventArgs(DataGridRow row, FrameworkElement detailsElement)
        {
            Row = row;
            DetailsElement = detailsElement;
        }

        public FrameworkElement DetailsElement 
        { 
            get; private set; 
        }

        public DataGridRow Row 
        { 
            get; private set; 
        }
    }
}
