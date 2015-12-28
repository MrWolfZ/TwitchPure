using ReactiveUI;

namespace TwitchPure.UI.ViewModels.Controls
{
  public sealed class StreamListViewModel : ReactiveObject
  {
    public StreamListViewModel(InfiniteReactiveList<StreamThumbnailViewModel>.LoadItemsAsync loadItemsAsync)
    {
      this.Streams = new InfiniteReactiveList<StreamThumbnailViewModel>(loadItemsAsync);
    }

    public InfiniteReactiveList<StreamThumbnailViewModel> Streams { get; }
  }
}
