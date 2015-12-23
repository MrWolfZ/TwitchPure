﻿using TwitchPure.UI.ViewModels;

namespace TwitchPure.UI.Views
{
  [View(ViewTokens.Browse, typeof(BrowseViewModel))]
  public sealed partial class Browse : IViewModelAwarePage<BrowseViewModel>
  {
    public Browse()
    {
      this.InitializeComponent();
    }

    public BrowseViewModel ViewModel => (BrowseViewModel)this.DataContext;
  }
}
