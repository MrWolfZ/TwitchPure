using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.System.Profile;
using Windows.UI.ViewManagement;
using MetroLog;
using Newtonsoft.Json;
using Prism.Windows.Navigation;
using ReactiveUI;
using TwitchPure.UI.ViewModels.Controls;

namespace TwitchPure.UI.ViewModels.Watch
{
  public sealed class LiveViewModel : ReactiveObject, INavigationAware
  {
    private readonly ILogger log;

    private readonly ObservableAsPropertyHelper<Uri> mediaUri;
    private readonly ObservableAsPropertyHelper<StreamQualityInfoCollection> qualities;
    private readonly ObservableAsPropertyHelper<bool> isCommandBarVisibile;
    private readonly TaskCompletionSource<string> channelName = new TaskCompletionSource<string>();

    public LiveViewModel(ILogger log)
    {
      this.log = log;

      this.qualities = Observable.FromAsync(this.GetQualities)
                                 .Catch((Exception ex) => Observable.Never<StreamQualityInfoCollection>())
                                 .ToProperty(this, vm => vm.Qualities, scheduler: RxApp.MainThreadScheduler);

      this.mediaUri = this.WhenAnyValue(vm => vm.Qualities)
                          .Where(col => col != null)
                          .Select(col => col.SelectedQuality)
                          .Switch()
                          .Select(info => new Uri(info.Url))
                          .ToProperty(this, x => x.MediaUri, scheduler: RxApp.MainThreadScheduler);

      this.SelectedQualityType = this.WhenAnyValue(vm => vm.Qualities)
                                     .Where(col => col != null)
                                     .Select(col => col.SelectedQuality)
                                     .Switch()
                                     .Select(info => info.Type);

      this.Tapped = ReactiveCommand.Create(Observable.Return(true));
      this.QualityMenuOpened = ReactiveCommand.Create(Observable.Return(true));

      this.isCommandBarVisibile = this.Tapped
                                      .Select(o => !this.IsCommandBarVisibile)
                                      .Publish(
                                        pub =>
                                        pub.SelectMany(
                                          b =>
                                          !b
                                            ? Observable.Return(false)
                                            : Observable.Return(true)
                                                        .Concat(
                                                          pub.Skip(1)
                                                             .FirstAsync()
                                                             .Amb(Observable.Timer(TimeSpan.FromSeconds(3)).Select(_ => false))
                                                             .Amb(this.QualityMenuOpened.FirstAsync().Select(_ => true)))))
                                      .ToProperty(this, vm => vm.IsCommandBarVisibile, scheduler: RxApp.MainThreadScheduler);
    }

    public ReactiveCommand<object> Tapped { get; }
    public ReactiveCommand<object> QualityMenuOpened { get; }
    public IObservable<QualityType> SelectedQualityType { get; }
    public StreamQualityInfoCollection Qualities => this.qualities.Value;

    public Uri MediaUri => this.mediaUri.Value;
    public bool IsCommandBarVisibile => this.isCommandBarVisibile.Value;

    public void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
    {
      // TODO: abstract into service
      DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape | DisplayOrientations.LandscapeFlipped;
      ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);
      ApplicationView.GetForCurrentView().FullScreenSystemOverlayMode = FullScreenSystemOverlayMode.Standard;

      if (AnalyticsInfo.VersionInfo.DeviceFamily.Contains("Mobile"))
      {
        ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
      }

      var args = JsonConvert.DeserializeObject<LiveNavigationArgs>((string)e.Parameter);
      this.log.Trace(args.ChannelName);

      this.channelName.SetResult(args.ChannelName);
    }

    public void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
    {
      DisplayInformation.AutoRotationPreferences = DisplayOrientations.None;
    }

    private async Task<StreamQualityInfoCollection> GetQualities()
    {
      var channel = (await this.channelName.Task).ToLowerInvariant();

      var regex = new Regex("#EXT-X-MEDIA:.*NAME=\"(.*)\".*\\n.*\\n(.*)");

      var r = new Random();

      var c = new HttpClient();
      var res = await c.GetStringAsync($"http://api.twitch.tv/api/channels/{channel}/access_token");
      var a = JsonConvert.DeserializeObject<Answer>(res);
      var url =
        new Uri(
          $"http://usher.twitch.tv/api/channel/hls/{channel}.m3u8?player=twitchweb&&token={a.Token}&sig={a.Sig}&allow_audio_only=true&allow_source=true&type=any&p={r.Next(100000, 999999)}");

      var res2 = await c.GetStringAsync(url);

      var infos = regex.Matches(res2).Cast<Match>().Select(
        m => new QualityInfo
        {
          Type = (QualityType)Enum.Parse(typeof(QualityType), m.Groups[1].Value.Replace(" ", "")),
          Url = m.Groups[2].Value
        }).ToList();

      return new StreamQualityInfoCollection(infos, infos[0]);
    }

    [JsonObject]
    public sealed class Answer
    {
      [JsonProperty("token")]
      public string Token { get; set; }

      [JsonProperty("sig")]
      public string Sig { get; set; }
    }
  }

  [DataContract]
  public class LiveNavigationArgs : NavigationArgs
  {
    [DataMember]
    public string ChannelName { get; set; }
  }

  public sealed class StreamQualityInfoCollection : ReactiveObject
  {
    private readonly ICollection<QualityInfo> availableQualities;
    private readonly ObservableAsPropertyHelper<bool> isSourceSelected;
    private readonly ObservableAsPropertyHelper<bool> isHighSelected;
    private readonly ObservableAsPropertyHelper<bool> isMediumSelected;
    private readonly ObservableAsPropertyHelper<bool> isLowSelected;
    private readonly ObservableAsPropertyHelper<bool> isMobileSelected;
    private readonly ObservableAsPropertyHelper<bool> isAudioOnlySelected;

    public StreamQualityInfoCollection(IEnumerable<QualityInfo> availableQualities, QualityInfo initialSelected)
    {
      this.availableQualities = availableQualities.ToList();

      this.SetSelectedQuality = ReactiveCommand.CreateAsyncObservable(p => Observable.Return((QualityInfo)p));
      this.SelectedQuality = this.SetSelectedQuality.StartWith(initialSelected);

      this.SetUpType(QualityType.Source, initialSelected.Type, col => col.IsSourceSelected, out this.isSourceSelected);
      this.SetUpType(QualityType.High, initialSelected.Type, col => col.IsHighSelected, out this.isHighSelected);
      this.SetUpType(QualityType.Medium, initialSelected.Type, col => col.IsMediumSelected, out this.isMediumSelected);
      this.SetUpType(QualityType.Low, initialSelected.Type, col => col.IsLowSelected, out this.isLowSelected);
      this.SetUpType(QualityType.Mobile, initialSelected.Type, col => col.IsMobileSelected, out this.isMobileSelected);
      this.SetUpType(QualityType.AudioOnly, initialSelected.Type, col => col.IsAudioOnlySelected, out this.isAudioOnlySelected);
    }

    public ReactiveCommand<QualityInfo> SetSelectedQuality { get; }
    public IObservable<QualityInfo> SelectedQuality { get; }

    public bool IsSourceVisible => this.availableQualities.Any(info => info.Type == QualityType.Source);
    public QualityInfo Source => this.availableQualities.SingleOrDefault(info => info.Type == QualityType.Source);
    public bool IsSourceSelected => this.isSourceSelected.Value;

    public bool IsHighVisible => this.availableQualities.Any(info => info.Type == QualityType.High);
    public QualityInfo High => this.availableQualities.SingleOrDefault(info => info.Type == QualityType.High);
    public bool IsHighSelected => this.isHighSelected.Value;

    public bool IsMediumVisible => this.availableQualities.Any(info => info.Type == QualityType.Medium);
    public QualityInfo Medium => this.availableQualities.SingleOrDefault(info => info.Type == QualityType.Medium);
    public bool IsMediumSelected => this.isMediumSelected.Value;

    public bool IsLowVisible => this.availableQualities.Any(info => info.Type == QualityType.Low);
    public QualityInfo Low => this.availableQualities.SingleOrDefault(info => info.Type == QualityType.Low);
    public bool IsLowSelected => this.isLowSelected.Value;

    public bool IsMobileVisible => this.availableQualities.Any(info => info.Type == QualityType.Mobile);
    public QualityInfo Mobile => this.availableQualities.SingleOrDefault(info => info.Type == QualityType.Mobile);
    public bool IsMobileSelected => this.isMobileSelected.Value;

    public bool IsAudioOnlyVisible => this.availableQualities.Any(info => info.Type == QualityType.AudioOnly);
    public QualityInfo AudioOnly => this.availableQualities.SingleOrDefault(info => info.Type == QualityType.AudioOnly);
    public bool IsAudioOnlySelected => this.isAudioOnlySelected.Value;

    private void SetUpType(
      QualityType type,
      QualityType initial,
      Expression<Func<StreamQualityInfoCollection, bool>> isSelectedProp,
      out ObservableAsPropertyHelper<bool> isSelectedField)
    {
      this.SelectedQuality
          .Select(info => info.Type == type)
          .ToProperty(this, isSelectedProp, out isSelectedField, initial == type, RxApp.MainThreadScheduler);
    }
  }

  public enum QualityType
  {
    Source = 1,
    High = 2,
    Medium = 3,
    Low = 4,
    Mobile = 5,
    AudioOnly = 6
  }

  public sealed class QualityInfo
  {
    public QualityType Type { get; set; }
    public string Url { get; set; }
  }
}
