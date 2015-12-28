using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Windows.UI.Xaml.Controls;
using Prism.Windows.Navigation;
using ReactiveUI;

namespace TwitchPure.UI.ViewModels.Controls
{
  public sealed class ShellViewModel : ReactiveObject, IDisposable
  {
    private readonly IDictionary<string, NavLink> topNavLinks = new Dictionary<string, NavLink>
    {
      { ViewToken.Favorites, new NavLink { Label = "Navbar.Favorites", Symbol = Symbol.Favorite, ViewToken = ViewToken.Favorites } },
      { ViewToken.TopStreams, new NavLink { Label = "Navbar.TopStreams", Symbol = Symbol.Admin, ViewToken = ViewToken.TopStreams } }
    };

    // ReSharper disable once CollectionNeverQueried.Local
    private readonly IDictionary<string, NavLink> bottomNavLinks = new Dictionary<string, NavLink>
    {
      { ViewToken.Settings, new NavLink { Label = "Navbar.Settings", Symbol = Symbol.Setting, ViewToken = ViewToken.Settings } }
    };

    private readonly ObservableAsPropertyHelper<ListViewSelectionMode> topListSelectionMode;
    private readonly ObservableAsPropertyHelper<ListViewSelectionMode> bottomListSelectionMode;
    private readonly CompositeDisposable disposable = new CompositeDisposable();
    private NavLink topSelectedLink;
    private NavLink bottomSelectedLink;

    public ShellViewModel(INavigationService navigationService, IFrameFacade frame)
    {
      this.TopNavLinks = this.topNavLinks.Values;
      this.BottomNavLinks = this.bottomNavLinks.Values;

      this.disposable.Add(
        this.WhenAny(vm => vm.TopSelectedLink, c => c.Value)
            .Merge(this.WhenAny(vm => vm.BottomSelectedLink, c => c.Value))
            .Where(link => link != null)
            .DistinctUntilChanged()
            .Select(link => link.ViewToken)
            .Subscribe(t => navigationService.Navigate(t, new NavigationArgs { TargetViewToken = t })));

      this.topListSelectionMode = this.WhenAny(vm => vm.BottomSelectedLink, c => c)
                                      .Where(c => c.Value != null)
                                      .SelectMany(c => Observable.Return(ListViewSelectionMode.None).Concat(Observable.Return(ListViewSelectionMode.Single)))
                                      .ToProperty(this, vm => vm.TopListSelectionMode, ListViewSelectionMode.Single);

      this.bottomListSelectionMode = this.WhenAny(vm => vm.TopSelectedLink, c => c)
                                         .Where(c => c.Value != null)
                                         .SelectMany(c => Observable.Return(ListViewSelectionMode.None).Concat(Observable.Return(ListViewSelectionMode.Single)))
                                         .ToProperty(this, vm => vm.BottomListSelectionMode, ListViewSelectionMode.Single);

      var navigations = Observable.FromEventPattern<NavigatedToEventArgs>(h => frame.NavigatedTo += h, h => frame.NavigatedTo -= h)
                                  .Select(args => args.EventArgs.Parameter)
                                  .Cast<NavigationArgs>()
                                  .Select(args => args.TargetViewToken)
                                  .Publish()
                                  .RefCount();

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

  public class NavigationArgs
  {
    public string TargetViewToken { get; set; }
    public string SourceViewToken { get; set; }
  }

  public sealed class NavLink
  {
    public string Label { get; set; }
    public Symbol Symbol { get; set; }
    public string ViewToken { get; set; }
  }
}
