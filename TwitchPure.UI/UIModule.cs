using System.Linq;
using System.Reflection;
using Autofac;
using Module = Autofac.Module;

namespace TwitchPure.UI
{
  public sealed class UIModule : Module
  {
    protected override void Load(ContainerBuilder builder)
    {
      var views = from t in this.GetType().GetTypeInfo().Assembly.GetTypes()
                  let a = t.GetTypeInfo().GetCustomAttribute<ViewAttribute>()
                  where a != null
                  select new ViewRegistration(a.Token, t, a.ViewModelType);

      foreach (var view in views)
      {
        builder.RegisterInstance(view);
      }

      base.Load(builder);
    }
  }
}
