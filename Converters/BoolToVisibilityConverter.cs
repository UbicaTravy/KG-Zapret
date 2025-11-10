using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace KG_Zapret.Converters {
    public class BoolToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is bool boolValue) {
                bool invert = parameter?.ToString() == "Inverted";
                bool result = invert ? !boolValue : boolValue;
                
                return result ? Visibility.Visible : Visibility.Collapsed;
            }
            
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is Visibility visibility) {
                bool result = visibility == Visibility.Visible;
                
                bool invert = parameter?.ToString() == "Inverted";
                return invert ? !result : result;
            }
            
            return false;
        }
    }
}

