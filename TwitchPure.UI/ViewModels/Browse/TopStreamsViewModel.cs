using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Data;
using ReactiveUI;
using TwitchPure.Services.Data.Twitch;
using TwitchPure.UI.ViewModels.Controls;

namespace TwitchPure.UI.ViewModels.Browse
{
  public sealed class TopStreamsViewModel : ReactiveObject
  {
    private readonly ITwitchApi twitchApi;

    public TopStreamsViewModel(ShellViewModel shellViewModel, ITwitchApi twitchApi)
    {
      this.twitchApi = twitchApi;
      shellViewModel.NestedDataContext = this;

      this.Streams = new InfiniteReactiveList<StreamThumbnailViewModel>(
        async (existingCount, requestedCount, token) =>
        {
          var response = await twitchApi.GetTopStreamsAsync(existingCount > 0 ? requestedCount : 10, existingCount);
          var hasMore = existingCount + response.Streams.Count < response.Total;
          var vms = response.Streams.Select(dto => new StreamThumbnailViewModel(dto));
          return Tuple.Create(vms, hasMore);
        }
        );
    }

    public InfiniteReactiveList<StreamThumbnailViewModel> Streams { get; }

    public class InfiniteReactiveList<T> : ReactiveList<T>, ISupportIncrementalLoading
    {
      private readonly Func<int, int, CancellationToken, Task<Tuple<IEnumerable<T>, bool>>> loadAsync;
      private bool isLoading;

      public InfiniteReactiveList(Func<int, int, CancellationToken, Task<Tuple<IEnumerable<T>, bool>>> loadAsync)
      {
        this.loadAsync = loadAsync;
      }

      public bool IsLoading
      {
        get { return this.isLoading; }
        set { this.RaiseAndSetIfChanged(ref this.isLoading, value); }
      }

      public bool HasMoreItems { get; private set; } = true;

      public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count) => AsyncInfo.Run(
        async c =>
        {
          this.IsLoading = true;

          try
          {
            var result = await this.loadAsync(this.Count, (int)count, c);
            this.HasMoreItems = result.Item2;
            var items = result.Item1.ToList();
            this.AddRange(items);

            return new LoadMoreItemsResult
            {
              Count = (uint)items.Count
            };
          }
          catch
          {
            // TODO: log exception
            this.HasMoreItems = false;
            return new LoadMoreItemsResult
            {
              Count = 0
            };
          }
          finally
          {
            this.IsLoading = false;
          }
        });
    }
  }
}
