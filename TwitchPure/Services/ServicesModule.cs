using Autofac;
using TwitchPure.Services.Data.Twitch;

namespace TwitchPure.Services
{
  public sealed class ServicesModule : Module
  {
    protected override void Load(ContainerBuilder builder)
    {
      builder.RegisterType<TwitchApi>()
             .AsImplementedInterfaces();

      builder.RegisterType<FavoritesService>()
             .AsImplementedInterfaces();

      base.Load(builder);
    }
  }
}