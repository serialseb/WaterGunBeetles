using System;
using System.Linq;
using System.Reflection;

namespace WaterGunBeetles.Internal
{
  public class MetaModelFactory
  {
    public static BeetlesMetaModel FromAssembly(Assembly assembly, string name)
    {
      var settingsType = assembly.ExportedTypes
                           .FirstOrDefault(t => typeof(IBeetlesConfiguration).IsAssignableFrom(t) &&
                                                (name == null || string.Equals(name, t.Name,
                                                   StringComparison.OrdinalIgnoreCase)))
                         ?? throw new ArgumentException(
                           GetTypeNotFoundMessage(name));
      return FromType(settingsType);
    }

    static string GetTypeNotFoundMessage(string name)
    {
      var message = $"Could not find a type implementing {typeof(IBeetlesOptions)}";
      if (name != null)
        message += $" named {name}";
      return message;
    }

    public static BeetlesMetaModel FromType(Type settingsType)
    {
      var options = new BeetlesOptions
      {
        MetaModel =
        {
          Name = settingsType.Name,
          ConfigurationType = settingsType
        }
      };
      var configurator = (IBeetlesConfiguration) Activator.CreateInstance(settingsType);
      configurator.Configure(options);
      return options.MetaModel;
    }
  }
}