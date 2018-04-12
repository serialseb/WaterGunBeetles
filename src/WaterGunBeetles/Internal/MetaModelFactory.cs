using System;
using System.Linq;
using System.Reflection;

namespace WaterGunBeetles.Internal
{
  public class MetaModelFactory
  {
    public static (BeetlesMetaModel model, Type settingsType) FromAssembly(Assembly assembly)
    {
      var settingsType = assembly.ExportedTypes
                           .FirstOrDefault(typeof(IBeetlesConfiguration).IsAssignableFrom)
                         ?? throw new ArgumentException(
                           $"Could not find a type implementing {typeof(IBeetlesOptions)}");
      var model = FromType(settingsType);
      return (model, settingsType);
    }

    public static BeetlesMetaModel FromType(Type settingsType)
    {
      var options = new BeetlesOptions {MetaModel = {Name = settingsType.Name}};
      var configurator = (IBeetlesConfiguration) Activator.CreateInstance(settingsType);
      configurator.Configure(options);
      return options.MetaModel;
    }
  }
}