using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace TwitchPure.UI.Converters
{
  public sealed class NullToVisibilityConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, string language) =>
      value != null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
      throw new NotSupportedException();
    }
  }
}
