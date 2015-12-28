using TwitchPure.UI.ViewModels.Controls;

namespace TwitchPure.UI.ViewModels.Settings
{
  public sealed class SettingsViewModel
  {
    public SettingsViewModel(ShellViewModel shellViewModel)
    {
      this.ShellViewModel = shellViewModel;
    }

    public ShellViewModel ShellViewModel { get; }
  }
}
