using TwitchPure.UI.ViewModels.Settings;

namespace TwitchPure.UI.Views.Settings
{
  [View(ViewToken.Settings, typeof(SettingsViewModel))]
  public sealed partial class Settings
  {
    public Settings()
    {
      this.InitializeComponent();
    }
  }
}
