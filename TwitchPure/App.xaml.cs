using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Autofac;
using Microsoft.ApplicationInsights;
using Prism.Mvvm;
using TwitchPure.UI;

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

      if (!string.IsNullOrEmpty(args?.Arguments))
      {
        // The app was launched from a Secondary Tile
        // Navigate to the item's page
        this.NavigationService.Navigate(ViewTokens.Browse, args.Arguments);
      }
      else
      {
        // Navigate to the initial page
        this.NavigationService.Navigate(ViewTokens.Browse, null);
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
      // builder.RegisterInstance<IResourceLoader>(new ResourceLoaderAdapter(new ResourceLoader()));

      builder.RegisterModule<UIModule>();

      base.ConfigureContainer(builder);
    }

    protected override Type GetPageType(string pageToken)
    {
      Type t;
      return this.ViewRegistrations.TryGetValue(pageToken, out t) ? t : base.GetPageType(pageToken);
    }

    protected override Task OnInitializeAsync(IActivatedEventArgs args)
    {
      var registrations = this.Container.Resolve<IEnumerable<ViewRegistration>>().ToDictionary(r => r.ViewType, r => r);
      foreach (var r in registrations.Values)
      {
        this.ViewRegistrations.Add(r.Token, r.ViewType);
      }

      ViewModelLocationProvider.SetDefaultViewTypeToViewModelTypeResolver(viewType => registrations[viewType].ViewModelType);

      // Documentation on working with tiles can be found at http://go.microsoft.com/fwlink/?LinkID=288821&clcid=0x409
      //var _tileUpdater = TileUpdateManager.CreateTileUpdaterForApplication();
      //_tileUpdater.StartPeriodicUpdate(new Uri(Constants.ServerAddress + "/api/TileNotification"), PeriodicUpdateRecurrence.HalfHour);

      return base.OnInitializeAsync(args);
    }
  }
}
