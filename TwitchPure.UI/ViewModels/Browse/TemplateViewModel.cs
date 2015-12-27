using System.Collections.ObjectModel;
using Windows.UI.Xaml.Controls;

namespace TwitchPure.UI.ViewModels.Browse
{
  public sealed class TemplateViewModel
  {
    public ObservableCollection<NavLink> NavLinks { get; } = new ObservableCollection<NavLink>
    {
      new NavLink { Label = "People", Symbol = Symbol.People },
      new NavLink { Label = "Globe", Symbol = Symbol.Globe },
      new NavLink { Label = "Message", Symbol = Symbol.Message },
      new NavLink { Label = "Mail", Symbol = Symbol.Mail }
    };

    public NavLink SelectedLink { get; set; }
  }
}
