using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Newtonsoft.Json;

namespace WaterGunBeetles
{
  public class LambdaControlPlane : IControlPlane
  {
    const int MaxConcurrentExecutionPerLambda = 200;

    public async Task SetLoad<TJourney>(LoadTestStepContext<TJourney> ctx)
    {
      var totalRequestsRoundedUp = (int) Math.Ceiling(ctx.RequestsPerSecond * ctx.Duration.TotalSeconds);

      var totalRequestsPerLambda = totalRequestsRoundedUp / MaxConcurrentExecutionPerLambda;

      var leftover = totalRequestsRoundedUp % MaxConcurrentExecutionPerLambda;

      var lambdaRequests = Enumerable.Range(0, MaxConcurrentExecutionPerLambda)
        .Select(pos => pos < leftover
          ? totalRequestsPerLambda + 1
          : totalRequestsPerLambda)
        .Where(count => count > 0)
        .Select(count => new AwsLambdaRequest<TJourney>
        {
          RequestCount = count,
          Journeys = ctx.TestScript.CreateJourneys(count).ToArray(),
          Duration = ctx.Duration
        });

      var publishRequests = lambdaRequests.Select((request, i) => new PublishRequest
      {
        TopicArn = _controlPlaneTopicArns[i % _controlPlaneTopicArns.Length],
        Message = JsonConvert.SerializeObject(request)
      });

      await ctx.PublishAsync(publishRequests, ctx.Cancel);
    }

    readonly string[] _controlPlaneTopicArns;

    public LambdaControlPlane(
      string[] controlPlaneTopicArns,
      Func<IEnumerable<PublishRequest>, CancellationToken, Task> publisher = null)
    {
      _controlPlaneTopicArns = controlPlaneTopicArns;
      Publisher = publisher ?? SnsPublisher;
    }

    static async Task SnsPublisher(IEnumerable<PublishRequest> publishRequests,
      CancellationToken cancellationToken)
    {
      using (var snsClient = new AmazonSimpleNotificationServiceClient(Amazon.RegionEndpoint.EUWest2))
      {
        foreach (var publishRequest in publishRequests)
          await snsClient.PublishAsync(publishRequest, cancellationToken);
      }
    }

    public Func<IEnumerable<PublishRequest>, CancellationToken, Task> Publisher { get; set; }
  }
}