using System.Linq;
using ReactiveUI;
using TwitchPure.Services.Data.Twitch;
using TwitchPure.UI.ViewModels.Controls;

namespace TwitchPure.UI.ViewModels.Browse
{
  public sealed class TopGamesViewModel : ReactiveObject
  {
    public TopGamesViewModel(ShellViewModel shellViewModel, ITwitchApi twitchApi)
    {
      shellViewModel.NestedDataContext = this;

      this.StreamListViewModel = new StreamListViewModel(
        async (existingCount, requestedCount, token) =>
        {
          var response = await twitchApi.GetTopStreamsAsync(requestedCount, existingCount);
          var hasMore = existingCount + response.Streams.Count < response.Total;
          var vms = response.Streams.Select(dto => new StreamThumbnailViewModel(dto)).ToList();
          return new InfiniteReactiveList<StreamThumbnailViewModel>.LoadItemsAsyncResult(vms, hasMore);
        }
        );
    }

    public StreamListViewModel StreamListViewModel { get; }
  }
}