using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Globalization;
using Windows.System.UserProfile;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Autofac;
using Microsoft.ApplicationInsights;
using Newtonsoft.Json;
using Prism.Logging;
using Prism.Mvvm;
using Prism.Windows.Navigation;
using TwitchPure.Services;
using TwitchPure.UI;
using TwitchPure.UI.ViewModels.Controls;

namespace TwitchPure
{
  /// <summary>
  ///   Provides application-specific behavior to supplement the default Application class.
  /// </summary>
  sealed partial class App
  {
    /// <summary>
    ///   Initializes the singleton application object.  This is the first line of authored code
    ///   executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
      WindowsAppInitializer.InitializeAsync(WindowsCollectors.Metadata | WindowsCollectors.Session);
      this.InitializeComponent();
    }

    private IDictionary<string, Type> ViewRegistrations { get; } = new Dictionary<string, Type>();

    protected override async Task OnLaunchApplicationAsync(LaunchActivatedEventArgs args)
    {
      await Task.Yield();

      ApplicationLanguages.PrimaryLanguageOverride = GlobalizationPreferences.Languages[0];

      if (!string.IsNullOrEmpty(args?.Arguments))
      {
        // The app was launched from a Secondary Tile
        // Navigate to the item's page
        // TODO: handle this
        this.NavigationService.Navigate(ViewToken.Favorites, args.Arguments);
      }
      else
      {
        // Navigate to the initial page
        const string InitialPage = ViewToken.TopChannels;
        this.NavigationService.Navigate(InitialPage, JsonConvert.SerializeObject(new NavigationArgs { TargetViewToken = InitialPage }));
      }

      Window.Current.Activate();
    }

    protected override void OnRegisterKnownTypesForSerialization()
    {
      // Set up the list of known types for the SuspensionManager
      this.SessionStateService.RegisterKnownType(typeof(Dictionary<string, Collection<string>>));
    }

    protected override void ConfigureContainer(ContainerBuilder builder)
    {
      builder.RegisterModule<LoggingModule>()
             .RegisterModule<UIModule>()
             .RegisterModule<ServicesModule>();

      base.ConfigureContainer(builder);
    }

    protected override async Task<Frame> InitializeFrameAsync(IActivatedEventArgs args)
    {
      var frame = await base.InitializeFrameAsync(args);

      this.RegisterInstance<IFrameFacade>(new FrameFacadeAdapter(frame), typeof(IFrameFacade), "", true);

      return frame;
    }

    protected override Type GetPageType(string pageToken)
    {
      Type t;
      return this.ViewRegistrations.TryGetValue(pageToken, out t) ? t : base.GetPageType(pageToken);
    }

    protected override Task OnInitializeAsync(IActivatedEventArgs args)
    {
      var viewRegistrations = this.Container.Resolve<IEnumerable<ViewRegistration>>().ToList();
      foreach (var r in viewRegistrations)
      {
        this.ViewRegistrations.Add(r.Token, r.ViewType);
      }

      var controlRegistrations = this.Container.Resolve<IEnumerable<ControlRegistration>>().ToList();

      var viewModelRegistry = viewRegistrations.Select(r => new { Type = r.ViewType, r.ViewModelType })
                                               .Concat(controlRegistrations.Select(r => new { Type = r.ControlType, r.ViewModelType }))
                                               .ToDictionary(a => a.Type, a => a.ViewModelType);

      ViewModelLocationProvider.SetDefaultViewTypeToViewModelTypeResolver(viewType => viewModelRegistry[viewType]);

      // Documentation on working with tiles can be found at http://go.microsoft.com/fwlink/?LinkID=288821&clcid=0x409
      //var _tileUpdater = TileUpdateManager.CreateTileUpdaterForApplication();
      //_tileUpdater.StartPeriodicUpdate(new Uri(Constants.ServerAddress + "/api/TileNotification"), PeriodicUpdateRecurrence.HalfHour);

      return base.OnInitializeAsync(args);
    }

    protected override ILoggerFacade CreateLogger() => new DebugLogger();

    private sealed class DebugLogger : ILoggerFacade
    {
      public void Log(string message, Category category, Priority priority)
      {
        var log = LoggingModule.LogManager.GetLogger<App>();
        switch (category)
        {
          case Category.Debug:
            log.Trace(message);
            break;
          case Category.Exception:
            log.Error(message);
            break;
          case Category.Info:
            log.Info(message);
            break;
          case Category.Warn:
            log.Warn(message);
            break;
          default:
            throw new ArgumentOutOfRangeException(nameof(category), category, null);
        }
      }
    }
  }
}
