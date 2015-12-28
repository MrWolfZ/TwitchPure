using System.Collections.Generic;
using System.Runtime.Serialization;
using MetroLog;
using Newtonsoft.Json;
using Prism.Windows.Navigation;
using ReactiveUI;
using TwitchPure.UI.ViewModels.Controls;

namespace TwitchPure.UI.ViewModels.Watch
{
  public sealed class LiveViewModel : ReactiveObject, INavigationAware
  {
    private readonly ILogger log;

    public LiveViewModel(ILogger log)
    {
      this.log = log;
    }

    public void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
    {
      var args = JsonConvert.DeserializeObject<LiveNavigationArgs>((string)e.Parameter);
      this.log.Trace(args.ChannelName);
    }

    public void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
    {
    }
  }

  [DataContract]
  public class LiveNavigationArgs : NavigationArgs
  {
    [DataMember]
    public string ChannelName { get; set; }
  }
}
