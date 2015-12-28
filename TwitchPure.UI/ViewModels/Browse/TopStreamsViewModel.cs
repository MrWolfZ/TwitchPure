using System.Collections.Generic;
using Prism.Windows.Navigation;
using ReactiveUI;
using TwitchPure.UI.ViewModels.Controls;

namespace TwitchPure.UI.ViewModels.Browse
{
  public sealed class TopStreamsViewModel : ReactiveObject, INavigationAware
  {
    private readonly INavigationService navigationService;

    public TopStreamsViewModel(NavbarViewModel navbarViewModel, INavigationService navigationService)
    {
      this.NavbarViewModel = navbarViewModel;
      this.navigationService = navigationService;
    }

    public NavbarViewModel NavbarViewModel { get; }

    public void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
    {
      this.navigationService.RemoveAllPages(ViewToken.Favorites);
    }

    public void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
    {
    }
  }
}
