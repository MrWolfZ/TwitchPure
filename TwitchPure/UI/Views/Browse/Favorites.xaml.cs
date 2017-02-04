using TwitchPure.UI.ViewModels.Browse;

namespace TwitchPure.UI.Views.Browse
{
  [View(ViewToken.Favorites, typeof(FavoritesViewModel))]
  public sealed partial class Favorites
  {
    public Favorites()
    {
      this.InitializeComponent();
    }
  }
}
