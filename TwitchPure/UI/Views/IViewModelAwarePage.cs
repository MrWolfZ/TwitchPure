namespace TwitchPure.UI.Views
{
  public interface IViewModelAwarePage<out T>
  {
    T ViewModel { get; }
  }
}
