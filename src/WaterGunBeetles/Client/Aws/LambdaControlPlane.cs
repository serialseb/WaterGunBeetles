using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Newtonsoft.Json;

namespace WaterGunBeetles.Client.Aws
{
  public class LambdaControlPlane : IControlPlane
  {
    const int MaxConcurrentExecutionPerLambda = 200;

    public async Task SetLoad(LoadTestStepContext ctx)
    {
      var totalRequestsRoundedUp = (int) Math.Ceiling(ctx.RequestsPerSecond * ctx.Duration.TotalSeconds);

      var totalRequestsPerLambdaInvocation = totalRequestsRoundedUp / MaxConcurrentExecutionPerLambda;

      var leftover = totalRequestsRoundedUp % MaxConcurrentExecutionPerLambda;

      var lambdaRequestCounts = Enumerable.Range(0, MaxConcurrentExecutionPerLambda)
        .Select(pos => pos < leftover
          ? totalRequestsPerLambdaInvocation + 1
          : totalRequestsPerLambdaInvocation)
        .Where(count => count > 0)
        .ToList();
      
      var publishRequests = new PublishRequest[lambdaRequestCounts.Count];

      for (var i = 0; i < lambdaRequestCounts.Count; i++)
      {
        var count = lambdaRequestCounts[i];
        publishRequests[i] = new PublishRequest
        {
          TopicArn = _controlPlaneTopicArns[i % _controlPlaneTopicArns.Length],
          Message = JsonConvert.SerializeObject(new LambdaRequest
          {
            RequestCount = count,
            Journeys = await ctx.StoryTeller(count),
            Duration = ctx.Duration
          })
        };
      }

      await ctx.PublishAsync(publishRequests, ctx.Cancel);
    }

    readonly string[] _controlPlaneTopicArns;
    readonly Action<object> _details;
    readonly Lazy<AmazonSimpleNotificationServiceClient> _snsClient;

    public LambdaControlPlane(
      string[] controlPlaneTopicArns,
      Func<IEnumerable<PublishRequest>, CancellationToken, Task> publisher = null,
      Action<object> detailsLog = null)
    {
      _controlPlaneTopicArns = controlPlaneTopicArns;
      _details = detailsLog ?? (_ => { });
      Publisher = publisher ?? SnsPublisher;
      _snsClient = new Lazy<AmazonSimpleNotificationServiceClient>(()=>new AmazonSimpleNotificationServiceClient(Amazon.RegionEndpoint.EUWest2));
    }

    async Task SnsPublisher(IEnumerable<PublishRequest> publishRequests, CancellationToken cancellationToken)
    {
      var sw = Stopwatch.StartNew();
        await Task.WhenAll(publishRequests.Select(r=>Task.Run(() => _snsClient.Value.PublishAsync(r, cancellationToken), cancellationToken)));

      _details($"[VERBOSE] Published {publishRequests.Count()} in {sw.Elapsed}");
    }

    public Func<IEnumerable<PublishRequest>, CancellationToken, Task> Publisher { get; set; }
  }
}