using System;
using System.Reactive.Linq;
using Prism.Logging;
using ReactiveUI;
using TwitchPure.Services.Data.Twitch;
using TwitchPure.Services.Dto.Twitch;
using TwitchPure.UI.ViewModels.Controls;

namespace TwitchPure.UI.ViewModels.Browse
{
  public sealed class TopStreamsViewModel : ReactiveObject
  {
    private readonly ITwitchApi twitchApi;

    public TopStreamsViewModel(ShellViewModel shellViewModel, ITwitchApi twitchApi, ILoggerFacade logger)
    {
      this.twitchApi = twitchApi;
      this.ShellViewModel = shellViewModel;
      this.ShellViewModel.NestedDataContext = this;

      Observable.FromAsync(twitchApi.GetTopStreamsAsync).ObserveOn(RxApp.MainThreadScheduler).Subscribe(streams => this.Streams.AddRange(streams));
    }

    public ShellViewModel ShellViewModel { get; }

    public string String { get; } = "wut";

    public IReactiveList<Stream> Streams { get; } = new ReactiveList<Stream>();
  }
}
