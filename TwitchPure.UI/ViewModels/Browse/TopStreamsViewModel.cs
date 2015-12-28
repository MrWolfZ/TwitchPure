using System;
using System.Linq;
using System.Reactive.Linq;
using Prism.Logging;
using ReactiveUI;
using TwitchPure.Services.Data.Twitch;
using TwitchPure.UI.ViewModels.Controls;

namespace TwitchPure.UI.ViewModels.Browse
{
  public sealed class TopStreamsViewModel : ReactiveObject
  {
    private readonly ITwitchApi twitchApi;

    public TopStreamsViewModel(ShellViewModel shellViewModel, ITwitchApi twitchApi, ILoggerFacade logger)
    {
      this.twitchApi = twitchApi;
      shellViewModel.NestedDataContext = this;

      Observable.FromAsync(twitchApi.GetTopStreamsAsync)
                .Select(col => col.Select(dto => new StreamThumbnailViewModel(dto)))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(streams => this.Streams.AddRange(streams));
    }

    public string String { get; } = "wut";

    public IReactiveList<StreamThumbnailViewModel> Streams { get; } = new ReactiveList<StreamThumbnailViewModel>();
  }
}
