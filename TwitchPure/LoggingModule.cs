using System.Linq;
using System.Reflection;
using Autofac.Core;
using MetroLog;
using MetroLog.Targets;
using Module = Autofac.Module;

namespace TwitchPure
{
  internal sealed class LoggingModule : Module
  {
    public static ILogManager LogManager;

    static LoggingModule()
    {
      var loggingConfiguration = new LoggingConfiguration();
      loggingConfiguration.AddTarget(LogLevel.Trace, LogLevel.Fatal, new DebugTarget());
      LogManager = LogManagerFactory.CreateLogManager(loggingConfiguration);
    }

    protected override void AttachToComponentRegistration(IComponentRegistry componentRegistry, IComponentRegistration registration)
    {
      // Handle constructor parameters.
      registration.Preparing += OnComponentPreparing;

      // Handle properties.
      registration.Activated += (sender, e) => InjectLoggerProperties(e.Instance);
    }

    private static void InjectLoggerProperties(object instance)
    {
      var instanceType = instance.GetType();

      // Get all the injectable properties to set.
      // If you wanted to ensure the properties were only UNSET properties,
      // here's where you'd do it.
      var properties = instanceType
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.PropertyType == typeof(ILogger) && p.CanWrite && p.GetIndexParameters().Length == 0);

      // Set the properties located.
      foreach (var propToSet in properties)
      {
        propToSet.SetValue(instance, LogManager.GetLogger(instanceType), null);
      }
    }

    private static void OnComponentPreparing(object sender, PreparingEventArgs e)
    {
      e.Parameters = e.Parameters.Union(
        new[]
        {
          new ResolvedParameter(
          (p, i) => p.ParameterType == typeof(ILogger),
          (p, i) => LogManager.GetLogger(p.Member.DeclaringType)
          )
        });
    }
  }
}
