using System;
using System.Reactive.Disposables;
using Windows.UI.Xaml.Navigation;
using TwitchPure.UI.ViewModels.Watch;

namespace TwitchPure.UI.Views.Watch
{
  [View(ViewToken.Live, typeof(LiveViewModel))]
  public sealed partial class Live : IViewModelAwarePage<LiveViewModel>
  {
    private readonly SingleAssignmentDisposable disposable = new SingleAssignmentDisposable();

    public Live()
    {
      this.InitializeComponent();

      this.SourceQualityMenuItem.IsChecked = true;
    }

    public LiveViewModel ViewModel => (LiveViewModel)this.DataContext;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
      this.disposable.Disposable = this.ViewModel.SelectedQualityType.Subscribe(this.ResetQualityMenuItemsThenSet);

      base.OnNavigatedTo(e);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
      this.disposable.Dispose();

      base.OnNavigatedFrom(e);
    }

    private void ResetQualityMenuItemsThenSet(QualityType type)
    {
      this.SourceQualityMenuItem.IsChecked = false;
      this.HighQualityMenuItem.IsChecked = false;
      this.MediumQualityMenuItem.IsChecked = false;
      this.LowQualityMenuItem.IsChecked = false;
      this.MobileQualityMenuItem.IsChecked = false;
      this.AudioOnlyQualityMenuItem.IsChecked = false;

      switch (type)
      {
        case QualityType.Source:
          this.SourceQualityMenuItem.IsChecked = true;
          break;
        case QualityType.High:
          this.HighQualityMenuItem.IsChecked = true;
          break;
        case QualityType.Medium:
          this.MediumQualityMenuItem.IsChecked = true;
          break;
        case QualityType.Low:
          this.LowQualityMenuItem.IsChecked = true;
          break;
        case QualityType.Mobile:
          this.MobileQualityMenuItem.IsChecked = true;
          break;
        case QualityType.AudioOnly:
          this.AudioOnlyQualityMenuItem.IsChecked = true;
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(type), type, null);
      }
    }
  }
}
