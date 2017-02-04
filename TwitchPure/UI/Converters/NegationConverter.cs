﻿using System;
using Windows.UI.Xaml.Data;

namespace TwitchPure.UI.Converters
{
  public sealed class NegationConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, string language) => !(value is bool) || !(bool)value;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
      throw new NotSupportedException();
    }
  }
}