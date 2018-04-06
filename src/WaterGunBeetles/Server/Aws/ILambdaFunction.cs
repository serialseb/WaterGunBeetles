using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;

namespace WaterGunBeetles.Server.Aws
{
  public interface ILambdaFunction
  {
    Task Handle(SNSEvent snsEvent, ILambdaContext context);
  }
}