using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Newtonsoft.Json;
using Prism.Windows.Navigation;
using ReactiveUI;
using TwitchPure.UI.ViewModels.Watch;
using UWP.Base;

namespace TwitchPure.UI.ViewModels.Controls
{
  public sealed class StreamListViewModel : ReactiveObject, IDisposable
  {
    private readonly CompositeDisposable disposables = new CompositeDisposable();
    private readonly ObservableAsPropertyHelper<bool> isSpinnerVisible;
    private readonly ObservableAsPropertyHelper<bool> isResultEmptyMessageVisible;
    private StreamThumbnailViewModel selectedItem;

    public StreamListViewModel(
      string viewToken,
      InfiniteReactiveList<StreamThumbnailViewModel>.LoadItemsAsync loadItemsAsync,
      INavigationService navigationService,
      IApplicationLifecycle appLifecycle)
    {
      this.Streams = new InfiniteReactiveList<StreamThumbnailViewModel>(loadItemsAsync);

      this.disposables.Add(appLifecycle.Suspends.Subscribe(u => this.Streams.Clear()));
      this.disposables.Add(appLifecycle.Resumes.Subscribe(u => this.Streams.Reset()));

      this.WhenAny(vm => vm.SelectedItem, c => c.Value)
          .FirstAsync(v => v != null)
          .Select(v => v.Source.Channel.DisplayName)
          .Subscribe(
            name =>
              navigationService.Navigate(
                ViewToken.Live,
                JsonConvert.SerializeObject(new LiveNavigationArgs { TargetViewToken = ViewToken.Live, SourceViewToken = viewToken, ChannelName = name })));

      this.isSpinnerVisible = this.Streams
                                  .WhenAnyValue(s => s.Count, s => s.HasMoreItems)
                                  .Select(t => t.Item1 == 0 && t.Item2)
                                  .ToProperty(this, vm => vm.IsSpinnerVisible, true);

      this.isResultEmptyMessageVisible = this.Streams
                                             .WhenAnyValue(s => s.Count, s => s.HasMoreItems)
                                             .Select(t => t.Item1 == 0 && !t.Item2)
                                             .ToProperty(this, vm => vm.IsResultEmptyMessageVisible);

      this.disposables.Add(this.isSpinnerVisible);
      this.disposables.Add(this.isResultEmptyMessageVisible);
    }

    public InfiniteReactiveList<StreamThumbnailViewModel> Streams { get; }

    public bool IsSpinnerVisible => this.isSpinnerVisible.Value;

    public bool IsResultEmptyMessageVisible => this.isResultEmptyMessageVisible.Value;

    public StreamThumbnailViewModel SelectedItem
    {
      get { return this.selectedItem; }
      set { this.RaiseAndSetIfChanged(ref this.selectedItem, value); }
    }

    public void Dispose() => this.disposables.Dispose();
  }
}
