using TwitchPure.UI.ViewModels.Browse;
using UWP.Base;

namespace TwitchPure.UI.Views.Browse
{
  [View(ViewToken.TopGames, typeof(TopGamesViewModel))]
  public sealed partial class TopGames : IViewModelAwarePage<TopGamesViewModel>
  {
    public TopGames()
    {
      this.InitializeComponent();
    }

    public TopGamesViewModel ViewModel => (TopGamesViewModel)this.DataContext;
  }
}
