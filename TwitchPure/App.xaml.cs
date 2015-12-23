using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Resources;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Autofac;
using Microsoft.ApplicationInsights;
using Microsoft.VisualBasic;
using Prism.Mvvm;
using Prism.Windows.AppModel;

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

    protected override async Task OnLaunchApplicationAsync(LaunchActivatedEventArgs args)
    {
      await Task.Yield();

      if (!string.IsNullOrEmpty(args?.Arguments))
      {
        // The app was launched from a Secondary Tile
        // Navigate to the item's page
        this.NavigationService.Navigate("ItemDetail", args.Arguments);
      }
      else
      {
        // Navigate to the initial page
        this.NavigationService.Navigate("Hub", null);
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
      builder.RegisterInstance<IResourceLoader>(new ResourceLoaderAdapter(new ResourceLoader()));

      base.ConfigureContainer(builder);
    }

    protected override Type GetPageType(string pageToken)
    {
      return base.GetPageType(pageToken);
    }

    protected override Task OnInitializeAsync(IActivatedEventArgs args)
    {
      ViewModelLocationProvider.SetDefaultViewTypeToViewModelTypeResolver(
        viewType =>
        {
          var viewModelTypeName = string.Format(
            CultureInfo.InvariantCulture,
            "AdventureWorks.UILogic.ViewModels.{0}ViewModel, AdventureWorks.UILogic, Version=1.1.0.0, Culture=neutral",
            viewType.Name);
          var viewModelType = Type.GetType(viewModelTypeName);
          if (viewModelType == null)
          {
            viewModelTypeName = string.Format(
              CultureInfo.InvariantCulture,
              "AdventureWorks.UILogic.ViewModels.{0}ViewModel, AdventureWorks.UILogic.Windows, Version=1.0.0.0, Culture=neutral",
              viewType.Name);
            viewModelType = Type.GetType(viewModelTypeName);
          }

          return viewModelType;
        });

      // Documentation on working with tiles can be found at http://go.microsoft.com/fwlink/?LinkID=288821&clcid=0x409
      //var _tileUpdater = TileUpdateManager.CreateTileUpdaterForApplication();
      //_tileUpdater.StartPeriodicUpdate(new Uri(Constants.ServerAddress + "/api/TileNotification"), PeriodicUpdateRecurrence.HalfHour);

      return base.OnInitializeAsync(args);
    }
  }
}
