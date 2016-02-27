using System;
using System.Collections.Generic;
using System.Linq;
using Prism.Windows.Navigation;
using ReactiveUI;
using TwitchPure.Services.Data.Twitch;
using TwitchPure.UI.ViewModels.Controls;

namespace TwitchPure.UI.ViewModels.Browse
{
  public sealed class TopGamesViewModel : ReactiveObject, INavigationAware, IDisposable
  {
    public TopGamesViewModel(
      ShellViewModel shellViewModel,
      Func<string, InfiniteReactiveList<StreamThumbnailViewModel>.LoadItemsAsync, StreamListViewModel> streamListViewModelFactory,
      ITwitchApi twitchApi)
    {
      shellViewModel.NestedDataContext = this;

      this.StreamListViewModel = streamListViewModelFactory(
        ViewToken.TopGames,
        async (existingCount, requestedCount, token) =>
        {
          var response = await twitchApi.GetTopStreamsAsync(Math.Max(10, requestedCount), existingCount);
          var hasMore = existingCount + response.Streams.Count < response.Total;
          var vms = response.Streams.Select(dto => new StreamThumbnailViewModel(dto)).ToList();
          return new InfiniteReactiveList<StreamThumbnailViewModel>.LoadItemsAsyncResult(vms, hasMore);
        }
        );
    }

    public StreamListViewModel StreamListViewModel { get; }

    public void Dispose()
    {
      foreach (var viewModel in this.StreamListViewModel.Streams)
      {
        viewModel.Dispose();
      }

      this.StreamListViewModel.Dispose();
    }

    public void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
    {
    }

    public void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
    {
      if (suspending)
      {
        return;
      }

      this.Dispose();
    }
  }
}
