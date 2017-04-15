using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive;
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
using Windows.UI.Xaml;
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
    private readonly ObservableAsPropertyHelper<List<QualityInfo>> qualities;
    private readonly ObservableAsPropertyHelper<bool> isCommandBarVisibile;
    private readonly ObservableAsPropertyHelper<bool> isMuted;
    private readonly ObservableAsPropertyHelper<bool> isSleepTimerActive;
    private readonly ObservableAsPropertyHelper<string> sleepTimerCountdown;
    private readonly ObservableAsPropertyHelper<bool> isFavorite;
    private readonly ObservableAsPropertyHelper<string> batteryChargeLevel;
    private readonly ObservableAsPropertyHelper<string> timeOfDay;
    private readonly TaskCompletionSource<List<QualityInfo>> qualityLoad = new TaskCompletionSource<List<QualityInfo>>();
    private readonly DeviceWatcher deviceWatcher;
    private string channelName;

    public LiveViewModel(ILogger log, IApplicationLifecycle appLifecycle, IFavoritesService favoritesService)
    {
      this.log = log;
      this.favoritesService = favoritesService;

      this.qualities = Observable.FromAsync(() => this.qualityLoad.Task)
                                 .Catch((Exception ex) => Observable.Never<List<QualityInfo>>())
                                 .ToProperty(this, vm => vm.Qualities, scheduler: RxApp.MainThreadScheduler);

      var suspendsAndResumes = appLifecycle.Suspends.Select(u => false)
                                           .Merge(appLifecycle.Resumes.Select(u => true))
                                           .StartWith(true);
      
      this.SetSelectedQualityType = ReactiveCommand.Create<QualityInfo, QualityInfo>(i => i);
      this.SelectedQualityType = this.WhenAnyValue(vm => vm.Qualities)
                                     .Where(col => col != null)
                                     .Select(col => col[0])
                                     .FirstAsync()
                                     .Concat(this.SetSelectedQualityType);

      this.mediaUri = this.WhenAnyValue(vm => vm.SelectedQualityType)
                          .Switch()
                          .Select(info => new Uri(info.Url))
                          .CombineLatest(suspendsAndResumes, (uri, b) => b ? uri : null)
                          .ToProperty(this, x => x.MediaUri, scheduler: RxApp.MainThreadScheduler);

      this.Tapped = ReactiveCommand.Create<Unit>(a => { });
      this.ToggleMute = ReactiveCommand.Create<Unit>(a => { });
      this.ToggleSleepTimer = ReactiveCommand.Create<Unit>(a => { });
      this.ToggleFavorite = ReactiveCommand.Create<Unit>(a => { });
      this.QualityMenuOpened = ReactiveCommand.Create<Unit>(a => { });

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

      this.disposables.Add(sleepTimerTriggered.ObserveOn(RxApp.MainThreadScheduler).Subscribe(l => Application.Current.Exit()));

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

    public ReactiveCommand<Unit, Unit> Tapped { get; }
    public ReactiveCommand<Unit, Unit> ToggleMute { get; }
    public ReactiveCommand<Unit, Unit> ToggleSleepTimer { get; }
    public ReactiveCommand<Unit, Unit> ToggleFavorite { get; }
    public ReactiveCommand<Unit, Unit> QualityMenuOpened { get; }
    public ReactiveCommand<QualityInfo, QualityInfo> SetSelectedQualityType { get; }
    public IObservable<QualityInfo> SelectedQualityType { get; }
    public List<QualityInfo> Qualities => this.qualities.Value;

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
        this.ToggleFavorite.Execute().Subscribe();
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

    private static async Task<List<QualityInfo>> GetQualities(string channel)
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

      return regex.Matches(res2).Cast<Match>().SelectMany(
        m => new[]
        {
          new QualityInfo
          {
            Type = m.Groups[1].Value.Replace(" ", ""),
            Url = m.Groups[2].Value
          }
        }).ToList();
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

  public sealed class QualityInfo
  {
    public string Type { get; set; }
    public string Url { get; set; }
  }
}
