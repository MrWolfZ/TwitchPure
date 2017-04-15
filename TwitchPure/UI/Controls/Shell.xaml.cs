using TwitchPure.UI.ViewModels.Controls;
using UWP.Base;

namespace TwitchPure.UI.Controls
{
  [Control(typeof(ShellViewModel))]
  public sealed partial class Shell
  {
    public Shell()
    {
      this.InitializeComponent();
    }
  }
}
