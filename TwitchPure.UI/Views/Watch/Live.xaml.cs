using TwitchPure.UI.ViewModels.Watch;

namespace TwitchPure.UI.Views.Watch
{
  [View(ViewToken.Live, typeof(LiveViewModel))]
  public sealed partial class Live
  {
    public Live()
    {
      this.InitializeComponent();
    }
  }
}
