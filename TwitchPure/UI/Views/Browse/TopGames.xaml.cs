using TwitchPure.UI.ViewModels.Browse;

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
