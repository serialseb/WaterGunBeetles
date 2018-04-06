using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using WaterGunBeetles.Internal;

namespace WaterGunBeetles.Server.Aws
{
  public class LambdaFunction : ILambdaFunction
  {
    static LambdaFunction()
    {
      var typeName = Environment.GetEnvironmentVariable(Constants.ConfigurationTypeNameKey);
      Model = MetaModelFactory.FromType(Type.GetType(typeName));
      LambdaType = typeof(LambdaFunction<,>).MakeGenericType(Model.JourneyType, Model.JourneyTypeResult);
      Factory = () => (ILambdaFunction)Activator.CreateInstance(LambdaType, Model);
    }

    public static Func<ILambdaFunction> Factory { get; set; }

    public static Type LambdaType { get; set; }

    static BeetlesMetaModel Model { get; set; }

    public Task Handle(SNSEvent snsEvent, ILambdaContext context)
    {
      return Factory().Handle(snsEvent, context);
    }
  }
}