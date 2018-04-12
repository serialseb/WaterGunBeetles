using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Newtonsoft.Json;
using WaterGunBeetles.Internal;

namespace WaterGunBeetles.Server.Aws
{
  public class LambdaFunction
  {
    static LambdaFunction()
    {
      var typeName = Environment.GetEnvironmentVariable(Constants.ConfigurationTypeNameKey);
      Model = MetaModelFactory.FromType(Type.GetType(typeName));
      LambdaType = typeof(LambdaFunction<,>).MakeGenericType(Model.JourneyType, Model.JourneyTypeResult);
      Factory = () => (ILambdaFunction) Activator.CreateInstance(LambdaType, Model);
    }

    public static Func<ILambdaFunction> Factory { get; set; }

    public static Type LambdaType { get; set; }

    static BeetlesMetaModel Model { get; }

    public Task Handle(Stream snsEvent, ILambdaContext context)
    {
      using (var sr = new StreamReader(snsEvent))
      using (JsonReader reader = new JsonTextReader(sr))
      {
        var msg = new JsonSerializer().Deserialize<SNSEvent>(reader);
        return Factory().Handle(msg, context);
      }
    }
  }
}