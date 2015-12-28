using System.Linq;
using System.Reflection;
using Windows.ApplicationModel.Resources;
using Autofac;
using Prism.Windows.AppModel;
using TwitchPure.UI.ViewModels.Controls;
using Module = Autofac.Module;

namespace TwitchPure.UI
{
  public sealed class UIModule : Module
  {
    protected override void Load(ContainerBuilder builder)
    {
      builder.Register<IResourceLoader>(c => new ResourceLoaderAdapter(ResourceLoader.GetForViewIndependentUse("TwitchPure.UI/Resources")));

      foreach (var view in from t in this.GetType().GetTypeInfo().Assembly.GetTypes()
                           let a = t.GetTypeInfo().GetCustomAttribute<ViewAttribute>()
                           where a != null
                           select new ViewRegistration(a.Token, t, a.ViewModelType))
      {
        builder.RegisterInstance(view);
      }

      foreach (var view in from t in this.GetType().GetTypeInfo().Assembly.GetTypes()
                           let a = t.GetTypeInfo().GetCustomAttribute<ControlAttribute>()
                           where a != null
                           select new ControlRegistration(t, a.ViewModelType))
      {
        builder.RegisterInstance(view);
      }

      builder.RegisterType<ShellViewModel>()
             .SingleInstance();

      base.Load(builder);
    }
  }
}
