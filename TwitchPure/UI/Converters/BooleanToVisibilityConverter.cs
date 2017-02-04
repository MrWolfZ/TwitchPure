using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace TwitchPure.UI.Converters
{
  public sealed class BooleanToVisibilityConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, string language) => GetVisibility(value);

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
      throw new NotSupportedException();
    }

    private static object GetVisibility(object value)
    {
      if (!(value is bool))
      {
        return Visibility.Collapsed;
      }

      var objValue = (bool)value;
      return objValue ? Visibility.Visible : Visibility.Collapsed;
    }
  }
}