using ReactiveUI;
using TwitchPure.UI.ViewModels.Controls;

namespace TwitchPure.UI.ViewModels.Browse
{
  public sealed class TopStreamsViewModel : ReactiveObject
  {
    public TopStreamsViewModel(ShellViewModel shellViewModel)
    {
      this.ShellViewModel = shellViewModel;
    }

    public ShellViewModel ShellViewModel { get; }
  }
}
