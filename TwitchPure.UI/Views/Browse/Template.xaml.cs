using TwitchPure.UI.ViewModels.Browse;

namespace TwitchPure.UI.Views.Browse
{
  [View(ViewTokens.Browse, typeof(TemplateViewModel))]
  public sealed partial class Template : IViewModelAwarePage<TemplateViewModel>
  {
    public Template()
    {
      this.InitializeComponent();
    }

    public TemplateViewModel ViewModel => (TemplateViewModel)this.DataContext;
  }
}
