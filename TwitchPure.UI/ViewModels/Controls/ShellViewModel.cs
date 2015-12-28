using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using Windows.UI.Xaml.Controls;
using MetroLog;
using Newtonsoft.Json;
using Prism.Windows.AppModel;
using Prism.Windows.Navigation;
using ReactiveUI;

namespace TwitchPure.UI.ViewModels.Controls
{
  public sealed class ShellViewModel : ReactiveObject, IDisposable
  {
    private readonly ILogger log;

    private readonly IDictionary<string, NavLink> topNavLinks = new Dictionary<string, NavLink>
    {
      { ViewToken.Favorites, new NavLink { Label = "Navbar_Favorites", Symbol = Symbol.OutlineStar, ViewToken = ViewToken.Favorites } },
      { ViewToken.TopGames, new NavLink { Label = "Navbar_TopGames", Symbol = Symbol.Caption, ViewToken = ViewToken.TopGames } },
      { ViewToken.TopChannels, new NavLink { Label = "Navbar_TopChannels", Symbol = Symbol.PreviewLink, ViewToken = ViewToken.TopChannels } }
    };

    private readonly IDictionary<string, NavLink> bottomNavLinks = new Dictionary<string, NavLink>
    {
      { ViewToken.Settings, new NavLink { Label = "Navbar_Settings", Symbol = Symbol.Setting, ViewToken = ViewToken.Settings } }
    };

    private readonly ObservableAsPropertyHelper<ListViewSelectionMode> topListSelectionMode;
    private readonly ObservableAsPropertyHelper<ListViewSelectionMode> bottomListSelectionMode;
    private readonly ObservableAsPropertyHelper<bool> isNavbarOpen;
    private readonly CompositeDisposable disposable = new CompositeDisposable();
    private NavLink topSelectedLink;
    private NavLink bottomSelectedLink;
    private object nestedDataContext;

    public ShellViewModel(INavigationService navigationService, IFrameFacade frame, ILogger log, IResourceLoader resourceLoader)
    {
      this.log = log;

      foreach (var navLink in this.topNavLinks.Values.Concat(this.bottomNavLinks.Values))
      {
        navLink.Label = resourceLoader.GetString(navLink.Label);
      }

      this.TopNavLinks = this.topNavLinks.Values;
      this.BottomNavLinks = this.bottomNavLinks.Values;

      this.isNavbarOpen = Observable.Return(true).ToProperty(this, vm => vm.IsNavbarOpen, true);

      var selections = this.WhenAny(vm => vm.TopSelectedLink, c => c.Value)
                           .Merge(this.WhenAny(vm => vm.BottomSelectedLink, c => c.Value))
                           .Where(link => link != null)
                           .Select(link => link.ViewToken);

      this.disposable.Add(
        selections
          .Publish(pub => pub.StartWith((string)null).Zip<string, string, Tuple<string, string>>(pub, Tuple.Create))
          .Select(t => new NavigationArgs { SourceViewToken = t.Item1, TargetViewToken = t.Item2 })
          .Do(t => this.log.Trace($"Navigating from '{t.SourceViewToken}' to '{t.TargetViewToken}'"))
          .Subscribe(args => navigationService.Navigate(args.TargetViewToken, JsonConvert.SerializeObject(args))));

      this.topListSelectionMode = this.WhenAny(vm => vm.BottomSelectedLink, c => c)
                                      .Where(c => c.Value != null)
                                      .SelectMany(c => Observable.Return(ListViewSelectionMode.None).Concat(Observable.Return(ListViewSelectionMode.Single)))
                                      .ToProperty(this, vm => vm.TopListSelectionMode, ListViewSelectionMode.Single);

      this.bottomListSelectionMode = this.WhenAny(vm => vm.TopSelectedLink, c => c)
                                         .Where(c => c.Value != null)
                                         .SelectMany(c => Observable.Return(ListViewSelectionMode.None).Concat(Observable.Return(ListViewSelectionMode.Single)))
                                         .ToProperty(this, vm => vm.BottomListSelectionMode, ListViewSelectionMode.Single);

      var navigations = from e in Observable.FromEventPattern<NavigatedToEventArgs>(h => frame.NavigatedTo += h, h => frame.NavigatedTo -= h)
                        let p = (string)e.EventArgs.Parameter
                        let o = JsonConvert.DeserializeObject<NavigationArgs>(p)
                        select o.TargetViewToken;

      this.disposable.Add(navigations.Subscribe(t => navigationService.RemoveAllPages(t)));

      this.disposable.Add(
        navigations
          .Where(t => this.topNavLinks.ContainsKey(t))
          .Subscribe(t => this.TopSelectedLink = this.topNavLinks[t]));

      this.disposable.Add(
        navigations
          .Where(t => this.bottomNavLinks.ContainsKey(t))
          .Subscribe(t => this.BottomSelectedLink = this.bottomNavLinks[t]));
    }

    public object NestedDataContext
    {
      get { return this.nestedDataContext; }
      set { this.RaiseAndSetIfChanged(ref this.nestedDataContext, value); }
    }

    public bool IsNavbarOpen => this.isNavbarOpen.Value;

    public ICollection<NavLink> TopNavLinks { get; }
    public ICollection<NavLink> BottomNavLinks { get; }

    public ListViewSelectionMode TopListSelectionMode => this.topListSelectionMode.Value;
    public ListViewSelectionMode BottomListSelectionMode => this.bottomListSelectionMode.Value;

    public NavLink TopSelectedLink
    {
      get { return this.topSelectedLink; }
      set { this.RaiseAndSetIfChanged(ref this.topSelectedLink, value); }
    }

    public NavLink BottomSelectedLink
    {
      get { return this.bottomSelectedLink; }
      set { this.RaiseAndSetIfChanged(ref this.bottomSelectedLink, value); }
    }

    public void Dispose() => this.disposable.Dispose();
  }

  [DataContract]
  public class NavigationArgs
  {
    [DataMember(Name = "TargetViewToken")]
    public string TargetViewToken { get; set; }

    [DataMember(Name = "SourceViewToken")]
    public string SourceViewToken { get; set; }
  }

  public sealed class NavLink
  {
    public string Label { get; set; }
    public Symbol Symbol { get; set; }
    public string ViewToken { get; set; }
  }
}
