using TwitchPure.UI.ViewModels.Browse;
using UWP.Base;

namespace TwitchPure.UI.Views.Browse
{
  [View(ViewToken.TopChannels, typeof(TopChannelsViewModel))]
  public sealed partial class TopChannels : IViewModelAwarePage<TopChannelsViewModel>
  {
    public TopChannels()
    {
      this.InitializeComponent();
    }

    public TopChannelsViewModel ViewModel => (TopChannelsViewModel)this.DataContext;
  }
}
