using TwitchPure.UI.ViewModels.Browse;
using UWP.Base;

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
