using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Power;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.System.Profile;
using Windows.UI.ViewManagement;
using MetroLog;
using Newtonsoft.Json;
using Prism.Windows.Navigation;
using ReactiveUI;
using TwitchPure.Services;
using TwitchPure.UI.ViewModels.Controls;

namespace TwitchPure.UI.ViewModels.Watch
{
  public sealed class LiveViewModel : ReactiveObject, INavigationAware, IDisposable
  {
    private readonly ILogger log;
    private readonly IFavoritesService favoritesService;

    private readonly CompositeDisposable disposables = new CompositeDisposable();
    private readonly ObservableAsPropertyHelper<Uri> mediaUri;
    private readonly ObservableAsPropertyHelper<double> volume;
    private readonly ObservableAsPropertyHelper<StreamQualityInfoCollection> qualities;
    private readonly ObservableAsPropertyHelper<bool> isCommandBarVisibile;
    private readonly ObservableAsPropertyHelper<bool> isMuted;
    private readonly ObservableAsPropertyHelper<bool> isSleepTimerActive;
    private readonly ObservableAsPropertyHelper<string> sleepTimerCountdown;
    private readonly ObservableAsPropertyHelper<bool> isFavorite;
    private readonly ObservableAsPropertyHelper<string> batteryChargeLevel;
    private readonly ObservableAsPropertyHelper<string> timeOfDay;
    private readonly TaskCompletionSource<StreamQualityInfoCollection> qualityLoad = new TaskCompletionSource<StreamQualityInfoCollection>();
    private readonly DeviceWatcher deviceWatcher;
    private string channelName;

    public LiveViewModel(ILogger log, IApplicationLifecycle appLifecycle, IFavoritesService favoritesService, INavigationService navigationService)
    {
      this.log = log;
      this.favoritesService = favoritesService;

      this.qualities = Observable.FromAsync(() => this.qualityLoad.Task)
                                 .Catch((Exception ex) => Observable.Never<StreamQualityInfoCollection>())
                                 .ToProperty(this, vm => vm.Qualities, scheduler: RxApp.MainThreadScheduler);

      var suspendsAndResumes = appLifecycle.Suspends.Select(u => false)
                                           .Merge(appLifecycle.Resumes.Select(u => true))
                                           .StartWith(true);

      this.mediaUri = this.WhenAnyValue(vm => vm.Qualities)
                          .Where(col => col != null)
                          .Select(col => col.SelectedQuality)
                          .Switch()
                          .Select(info => new Uri(info.Url))
                          .CombineLatest(suspendsAndResumes, (uri, b) => b ? uri : null)
                          .ToProperty(this, x => x.MediaUri, scheduler: RxApp.MainThreadScheduler);

      this.SelectedQualityType = this.WhenAnyValue(vm => vm.Qualities)
                                     .Where(col => col != null)
                                     .Select(col => col.SelectedQuality)
                                     .Switch()
                                     .Select(info => info.Type);

      this.Tapped = ReactiveCommand.Create(Observable.Return(true));
      this.ToggleMute = ReactiveCommand.Create(Observable.Return(true));
      this.ToggleSleepTimer = ReactiveCommand.Create(Observable.Return(true));
      this.ToggleFavorite = ReactiveCommand.Create(Observable.Return(true));
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

      this.deviceWatcher = DeviceInformation.CreateWatcher(DeviceClass.AudioRender);

      // TODO: abstract into service
      var muteOnHeadphonesRemoved = Observable.FromEventPattern<TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>, DeviceInformationUpdate>(
                                                h => this.deviceWatcher.Removed += h,
                                                h => this.deviceWatcher.Removed -= h
                                              ).Select(a => true).Publish();

      this.disposables.Add(muteOnHeadphonesRemoved.Connect());

      this.isMuted = this.ToggleMute
                         .Select(o => !this.IsMuted)
                         .Merge(muteOnHeadphonesRemoved)
                         .ToProperty(this, vm => vm.IsMuted, scheduler: RxApp.MainThreadScheduler);

      this.isSleepTimerActive = this.ToggleSleepTimer
                                    .Select(o => !this.IsSleepTimerActive)
                                    .ToProperty(this, vm => vm.IsSleepTimerActive, scheduler: RxApp.MainThreadScheduler);

      // TODO: make choosable via UI
      var sleepTimerDuration = TimeSpan.FromMinutes(30);
      var sleepTimerTriggered = this.WhenAnyValue(vm => vm.IsSleepTimerActive)
                                    .Select(b => b ? Observable.Timer(sleepTimerDuration) : Observable.Never<long>())
                                    .Switch();

      this.disposables.Add(sleepTimerTriggered.ObserveOn(RxApp.MainThreadScheduler).Subscribe(l => navigationService.GoBack()));

      this.sleepTimerCountdown = this.WhenAnyValue(vm => vm.IsSleepTimerActive)
                                     .Select(
                                       b =>
                                       {
                                         if (b)
                                         {
                                           // 1s offset since we want to round up for time display
                                           var targetTime = DateTime.Now + sleepTimerDuration + TimeSpan.FromSeconds(1);
                                           return Observable.Interval(TimeSpan.FromMilliseconds(100))
                                                            .Select(l => targetTime - DateTime.Now)
                                                            .Select(t => t.ToString(@"mm\:ss"));
                                         }

                                         return Observable.Never<string>();
                                       })
                                     .Switch()
                                     .ToProperty(this, vm => vm.SleepTimerCountdown, scheduler: RxApp.MainThreadScheduler);

      this.disposables.Add(this.sleepTimerCountdown);

      this.isFavorite = this.ToggleFavorite
                            .Select(o => !this.IsFavorite)
                            .ToProperty(this, vm => vm.IsFavorite, scheduler: RxApp.MainThreadScheduler);

      this.volume = this.WhenAnyValue(vm => vm.IsMuted)
                        .Select(b => b ? 0.0 : 1.0)
                        .ToProperty(this, vm => vm.Volume, scheduler: RxApp.MainThreadScheduler, initialValue: 1.0);

      var favoriteChanges = this.WhenAnyValue(vm => vm.IsFavorite)
                                .Where(b => !string.IsNullOrWhiteSpace(this.channelName));

      this.disposables.Add(favoriteChanges.Where(b => b).Select(b => this.channelName).Subscribe(favoritesService.AddChannelToFavorites));
      this.disposables.Add(favoriteChanges.Where(b => !b).Select(b => this.channelName).Subscribe(favoritesService.RemoveChannelFromFavorites));

      var battery = Battery.AggregateBattery;
      var batteryReportsUpdated = Observable.FromEventPattern<TypedEventHandler<Battery, object>, Battery, object>(
        h => battery.ReportUpdated += h,
        h => battery.ReportUpdated -= h);

      this.batteryChargeLevel = batteryReportsUpdated.Select(args => args.Sender)
                                                     .StartWith(battery)
                                                     .Select(b => b.GetReport())
                                                     .Select(r => r.RemainingCapacityInMilliwattHours / (double?)r.FullChargeCapacityInMilliwattHours)
                                                     .Select(l => (l ?? 1) * 100)
                                                     .Select(l => $"{Math.Round(l)}%")
                                                     .ToProperty(this, vm => vm.BatteryChargeLevel, scheduler: RxApp.MainThreadScheduler);

      this.timeOfDay = Observable.Interval(TimeSpan.FromSeconds(1))
                                 .StartWith(0)
                                 .Select(l => DateTime.Now.ToString("h:mm tt"))
                                 .ToProperty(this, vm => vm.TimeOfDay, scheduler: RxApp.MainThreadScheduler);

      this.disposables.Add(this.mediaUri);
      this.disposables.Add(this.volume);
      this.disposables.Add(this.qualities);
      this.disposables.Add(this.isCommandBarVisibile);
      this.disposables.Add(this.isMuted);
      this.disposables.Add(this.isSleepTimerActive);
      this.disposables.Add(this.isFavorite);
      this.disposables.Add(this.batteryChargeLevel);
      this.disposables.Add(this.timeOfDay);
    }

    public ReactiveCommand<object> Tapped { get; }
    public ReactiveCommand<object> ToggleMute { get; }
    public ReactiveCommand<object> ToggleSleepTimer { get; }
    public ReactiveCommand<object> ToggleFavorite { get; }
    public ReactiveCommand<object> QualityMenuOpened { get; }
    public IObservable<QualityType> SelectedQualityType { get; }
    public StreamQualityInfoCollection Qualities => this.qualities.Value;

    public Uri MediaUri => this.mediaUri.Value;
    public bool IsCommandBarVisibile => this.isCommandBarVisibile.Value;
    public double Volume => this.volume.Value;
    public string BatteryChargeLevel => this.batteryChargeLevel.Value;
    public string TimeOfDay => this.timeOfDay.Value;
    public bool IsMuted => this.isMuted.Value;
    public bool IsSleepTimerActive => this.isSleepTimerActive.Value;
    public string SleepTimerCountdown => this.sleepTimerCountdown.Value;
    public bool IsFavorite => this.isFavorite.Value;

    public void Dispose() => this.disposables.Dispose();

    public void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
    {
      // TODO: abstract into service
      DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape | DisplayOrientations.LandscapeFlipped;
      ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);
      ApplicationView.GetForCurrentView().FullScreenSystemOverlayMode = FullScreenSystemOverlayMode.Standard;

      this.deviceWatcher.Start();

      if (AnalyticsInfo.VersionInfo.DeviceFamily.Contains("Mobile"))
      {
        ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
      }

      var args = JsonConvert.DeserializeObject<LiveNavigationArgs>((string)e.Parameter);
      this.log.Trace(args.ChannelName);
      this.channelName = args.ChannelName;
      var isFav = this.favoritesService.IsChannelFavorite(args.ChannelName);

      if (isFav != this.IsFavorite)
      {
        this.ToggleFavorite.Execute(isFav);
      }

      Observable.FromAsync(() => GetQualities(args.ChannelName)).Subscribe(this.qualityLoad.SetResult);
    }

    public void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
    {
      if (suspending)
      {
        return;
      }

      DisplayInformation.AutoRotationPreferences = DisplayOrientations.None;
      ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseVisible);
      ApplicationView.GetForCurrentView().ExitFullScreenMode();

      this.deviceWatcher.Stop();
      this.Dispose();
    }

    private static async Task<StreamQualityInfoCollection> GetQualities(string channel)
    {
      channel = channel.ToLowerInvariant();

      var regex = new Regex("#EXT-X-MEDIA:.*NAME=\"(.*)\".*\\n.*\\n(.*)");

      var r = new Random();

      var c = new HttpClient();
      c.DefaultRequestHeaders.Add("Client-ID", "lsx8xunzjbcx15nwhjrjwbw7ryn81uv");
      var res = await c.GetStringAsync($"http://api.twitch.tv/api/channels/{channel}/access_token");
      var a = JsonConvert.DeserializeObject<Answer>(res);
      var url =
        new Uri(
          $"http://usher.twitch.tv/api/channel/hls/{channel}.m3u8?player=twitchweb&&token={a.Token}&sig={a.Sig}&allow_audio_only=true&allow_source=true&type=any&p={r.Next(100000, 999999)}");

      var res2 = await c.GetStringAsync(url);

      var infos = regex.Matches(res2).Cast<Match>().SelectMany(
                         m =>
                         {
                           try
                           {
                             var quality = m.Groups[1].Value.Replace(" ", "");

                             if (quality == "1080p60")
                             {
                               quality = "Source";
                             }
                             else if (quality == "720p60")
                             {
                               quality = "High";
                             }
                             else if (quality == "540p30")
                             {
                               quality = "Medium";
                             }

                             return new[]
                             {
                               new QualityInfo
                               {
                                 Type = (QualityType)Enum.Parse(typeof(QualityType), quality),
                                 Url = m.Groups[2].Value
                               }
                             };
                           }

                           catch (Exception)
                           {
                             return Enumerable.Empty<QualityInfo>();
                           }
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
