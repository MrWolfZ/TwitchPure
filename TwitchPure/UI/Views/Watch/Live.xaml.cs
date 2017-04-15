using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using ReactiveUI;
using TwitchPure.UI.ViewModels.Watch;
using UWP.Base;

namespace TwitchPure.UI.Views.Watch
{
  [View(ViewToken.Live, typeof(LiveViewModel))]
  public sealed partial class Live : IViewModelAwarePage<LiveViewModel>
  {
    private readonly CompositeDisposable disposable = new CompositeDisposable();

    public Live()
    {
      this.InitializeComponent();
    }

    public LiveViewModel ViewModel => (LiveViewModel)this.DataContext;

    private IEnumerable<ToggleMenuFlyoutItem> QualityMenuFlyoutItems
      => this.QualityMenuFlyout.Items?.Cast<ToggleMenuFlyoutItem>() ?? new List<ToggleMenuFlyoutItem>();

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
      this.disposable.Add(this.ViewModel.WhenAnyValue(vm => vm.Qualities).Where(col => col != null).Subscribe(this.RepopulateQualityMenu));
      this.disposable.Add(this.ViewModel.SelectedQualityType.Subscribe(this.ResetQualityMenuItemsThenSet));

      base.OnNavigatedTo(e);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
      this.disposable.Dispose();

      base.OnNavigatedFrom(e);
    }

    private void RepopulateQualityMenu(List<QualityInfo> qualityInfos)
    {
      this.QualityMenuFlyout.Items?.Clear();
      foreach (var item in qualityInfos.Select(this.CreateQualityMenuFlyoutItem))
      {
        this.QualityMenuFlyout.Items?.Add(item);
      }
    }

    private ToggleMenuFlyoutItem CreateQualityMenuFlyoutItem(QualityInfo qualityInfo)
    {
      return new ToggleMenuFlyoutItem
      {
        Text = qualityInfo.Type,
        Command = this.ViewModel.SetSelectedQualityType,
        CommandParameter = qualityInfo
      };
    }

    private void ResetQualityMenuItemsThenSet(QualityInfo qualityInfo)
    {
      foreach (var item in this.QualityMenuFlyoutItems)
      {
        item.IsChecked = item.Text == qualityInfo.Type;
      }
    }
  }
}
