using Autofac;
using TwitchPure.UI;
using UWP.Base;

namespace TwitchPure
{
  public sealed class AppModule : Module
  {
    protected override void Load(ContainerBuilder builder)
    {
      builder.RegisterInstance(new ApplicationConfiguration())
             .As<IApplicationConfiguration>();

      base.Load(builder);
    }

    private sealed class ApplicationConfiguration : IApplicationConfiguration
    {
      public string InitialPageToken => ViewToken.Favorites;
    }
  }
}
