using Autofac;
using TwitchPure.Services.Data.Twitch;

namespace TwitchPure.Services
{
  public sealed class ServicesModule : Module
  {
    protected override void Load(ContainerBuilder builder)
    {
      builder.RegisterType<TwitchApi>()
             .As<ITwitchApi>();

      base.Load(builder);
    }
  }
}