using System;

namespace TwitchPure.UI
{
  public sealed class ViewRegistration
  {
    public ViewRegistration(string token, Type viewType, Type viewModelType)
    {
      this.Token = token;
      this.ViewType = viewType;
      this.ViewModelType = viewModelType;
    }

    public string Token { get; }
    public Type ViewType { get; }
    public Type ViewModelType { get; }
  }
}
