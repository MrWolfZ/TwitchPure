using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Data;
using ReactiveUI;

namespace TwitchPure.UI
{
  public class InfiniteReactiveList<T> : ReactiveList<T>, ISupportIncrementalLoading
  {
    public delegate Task<LoadItemsAsyncResult> LoadItemsAsync(int existingItemCount, int requestedItemCount, CancellationToken token);

    private readonly LoadItemsAsync loadAsync;
    private bool isLoading;
    private bool hasMoreItems = true;

    public InfiniteReactiveList(LoadItemsAsync loadAsync)
    {
      this.loadAsync = loadAsync;
    }

    public bool IsLoading
    {
      get { return this.isLoading; }
      set { this.RaiseAndSetIfChanged(ref this.isLoading, value); }
    }

    public bool HasMoreItems
    {
      get { return this.hasMoreItems; }
      private set { this.RaiseAndSetIfChanged(ref this.hasMoreItems, value); }
    }

    public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count) => AsyncInfo.Run(c => this.ExecuteLoadItemsAsync(count, c));

    private async Task<LoadMoreItemsResult> ExecuteLoadItemsAsync(uint count, CancellationToken c)
    {
      this.IsLoading = true;

      try
      {
        var result = await this.loadAsync(this.Count, (int)count, c);
        this.HasMoreItems = result.HasMoreItems;
        this.AddRange(result.Items);

        return new LoadMoreItemsResult
        {
          Count = (uint)result.Items.Count
        };
      }
      catch
      {
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
    }

    public sealed class LoadItemsAsyncResult
    {
      public LoadItemsAsyncResult(ICollection<T> items, bool hasMoreItems)
      {
        this.Items = items;
        this.HasMoreItems = hasMoreItems;
      }

      public ICollection<T> Items { get; }
      public bool HasMoreItems { get; }
    }
  }
}
