using System;
using System.Reactive;
using System.Reactive.Linq;
using Windows.ApplicationModel;
using Windows.UI.Xaml;

namespace TwitchPure.Services
{
  public interface IApplicationLifecycle
  {
    IObservable<Unit> Suspends { get; }
    IObservable<Unit> Resumes { get; }
  }

  internal sealed class ApplicationLifecycle : IApplicationLifecycle
  {
    public ApplicationLifecycle()
    {
      var suspends = Observable.FromEventPattern<SuspendingEventHandler, SuspendingEventArgs>(
        h => Application.Current.Suspending += h,
        h => Application.Current.Suspending -= h);

      var resumes = Observable.FromEventPattern<object>(
        h => Application.Current.Resuming += h,
        h => Application.Current.Resuming -= h);

      this.Suspends = suspends.Select(e => Unit.Default).Publish().RefCount();
      this.Resumes = resumes.Select(e => Unit.Default).Publish().RefCount();
    }

    public IObservable<Unit> Suspends { get; }
    public IObservable<Unit> Resumes { get; }
  }
}
