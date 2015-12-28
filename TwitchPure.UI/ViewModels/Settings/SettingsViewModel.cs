using TwitchPure.UI.ViewModels.Controls;

namespace TwitchPure.UI.ViewModels.Settings
{
  public sealed class SettingsViewModel
  {
    public SettingsViewModel(NavbarViewModel navbarViewModel)
    {
      this.NavbarViewModel = navbarViewModel;
    }

    public NavbarViewModel NavbarViewModel { get; }
  }
}
