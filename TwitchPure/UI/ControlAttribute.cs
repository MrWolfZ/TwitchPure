using System;

namespace TwitchPure.UI
{
  [AttributeUsage(AttributeTargets.Class)]
  internal sealed class ControlAttribute : Attribute
  {
    public ControlAttribute(Type viewModelType)
    {
      this.ViewModelType = viewModelType;
    }

    public Type ViewModelType { get; }
  }
}