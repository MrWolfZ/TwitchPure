using ReactiveUI;
using TwitchPure.UI.ViewModels.Controls;

namespace TwitchPure.UI.ViewModels.Browse
{
  public sealed class FavoritesViewModel : ReactiveObject
  {
    public FavoritesViewModel(ShellViewModel shellViewModel)
    {
      this.ShellViewModel = shellViewModel;
    }

    public ShellViewModel ShellViewModel { get; }
  }
}
