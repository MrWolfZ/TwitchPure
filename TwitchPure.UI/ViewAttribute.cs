using System;

namespace TwitchPure.UI
{
  [AttributeUsage(AttributeTargets.Class)]
  internal sealed class ViewAttribute : Attribute
  {
    public ViewAttribute(string token, Type viewModelType)
    {
      this.Token = token;
      this.ViewModelType = viewModelType;
    }

    public string Token { get; }
    public Type ViewModelType { get; }
  }
}
