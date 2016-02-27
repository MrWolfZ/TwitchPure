using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Newtonsoft.Json;
using Prism.Windows.Navigation;
using ReactiveUI;
using TwitchPure.Services;
using TwitchPure.UI.ViewModels.Watch;

namespace TwitchPure.UI.ViewModels.Controls
{
  public sealed class StreamListViewModel : ReactiveObject, IDisposable
  {
    private readonly CompositeDisposable disposables = new CompositeDisposable();
    private StreamThumbnailViewModel selectedItem;

    public StreamListViewModel(
      string viewToken,
      InfiniteReactiveList<StreamThumbnailViewModel>.LoadItemsAsync loadItemsAsync,
      INavigationService navigationService,
      IApplicationLifecycle appLifecycle)
    {
      this.Streams = new InfiniteReactiveList<StreamThumbnailViewModel>(loadItemsAsync);

      this.disposables.Add(appLifecycle.Suspends.Subscribe(u => this.Streams.Clear()));

      this.WhenAny(vm => vm.SelectedItem, c => c.Value)
          .FirstAsync(v => v != null)
          .Select(v => v.Source.Channel.DisplayName)
          .Subscribe(
            name =>
            navigationService.Navigate(
              ViewToken.Live,
              JsonConvert.SerializeObject(new LiveNavigationArgs { TargetViewToken = ViewToken.Live, SourceViewToken = viewToken, ChannelName = name })));
    }

    public InfiniteReactiveList<StreamThumbnailViewModel> Streams { get; }

    public StreamThumbnailViewModel SelectedItem
    {
      get { return this.selectedItem; }
      set { this.RaiseAndSetIfChanged(ref this.selectedItem, value); }
    }

    public void Dispose() => this.disposables.Dispose();
  }
}
