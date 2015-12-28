using TwitchPure.UI.ViewModels.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

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
