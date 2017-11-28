using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace PlayLogger
{
    public sealed class VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility curr = Visibility.Collapsed;
            if (value is Visibility)
            {
                curr = (Visibility)value;
            }
            else 
            {
                Visibility trueVal = Visibility.Visible;
                Visibility falseVal = Visibility.Collapsed;
                if (parameter is Visibility)
                {
                    trueVal = (Visibility)parameter;
                    falseVal = (trueVal == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible;
                }
                else if (parameter is string)
                {
                    switch ((string)parameter)
                    {
                        case "Visible":
                            trueVal = Visibility.Visible;
                            break;
                        case "Collapsed":
                            trueVal = Visibility.Collapsed;
                            break;
                        case "Hidden":
                            trueVal = Visibility.Hidden;
                            break;
                        default:
                            trueVal = Visibility.Visible;
                            break;
                    }
                    falseVal = (trueVal == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible;
                }

                curr = System.Convert.ToBoolean(value) ? trueVal : falseVal;
            }

            return curr;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
