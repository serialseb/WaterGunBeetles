using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Newtonsoft.Json;

namespace WaterGunBeetles.Client.Aws
{
  public class LambdaControlPlane : IControlPlane
  {
    const int ProvisionedConcurrentExecutionForLambda = 600;

    readonly string _controlPlaneTopicArn;
    readonly Action<object> _details;
    readonly Lazy<AmazonSimpleNotificationServiceClient> _snsClient;

    public LambdaControlPlane(
      string controlPlaneTopicArn,
      Func<IEnumerable<PublishRequest>, CancellationToken, Task> publisher = null,
      Action<object> detailsLog = null)
    {
      _controlPlaneTopicArn = controlPlaneTopicArn;
      _details = detailsLog ?? (_ => { });
      Publisher = publisher ?? SnsPublisher;
      _snsClient = new Lazy<AmazonSimpleNotificationServiceClient>(() =>
        new AmazonSimpleNotificationServiceClient(RegionEndpoint.EUWest2));
    }

    public async Task SetLoad(LoadTestStepContext ctx)
    {
      var lambdaRequestCounts = JourneyCalcuations.JourneyCounts(
        ProvisionedConcurrentExecutionForLambda,
        ctx.RequestsPerSecond,
        ctx.Duration.TotalSeconds,
        12);

      var publishRequests = new PublishRequest[lambdaRequestCounts.Count];

      for (var i = 0; i < lambdaRequestCounts.Count; i++)
      {
        var count = lambdaRequestCounts[i];
        publishRequests[i] = new PublishRequest
        {
          TopicArn = _controlPlaneTopicArn,
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

    public Func<IEnumerable<PublishRequest>, CancellationToken, Task> Publisher { get; set; }

    async Task SnsPublisher(IEnumerable<PublishRequest> publishRequests, CancellationToken cancellationToken)
    {
      var sw = Stopwatch.StartNew();
      await Task.WhenAll(publishRequests.Select(r =>
        Task.Run(() => _snsClient.Value.PublishAsync(r, cancellationToken), cancellationToken)));

      _details($"[VERBOSE] Published {publishRequests.Count()} in {sw.Elapsed}");
    }
  }
}