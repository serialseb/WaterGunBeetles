using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;
using Newtonsoft.Json;
using Shouldly;
using Tests.Infrastructure;
using WaterGunBeetles;
using WaterGunBeetles.Client;
using WaterGunBeetles.Client.Aws;
using Xunit;

namespace Tests
{
  public class linear_ramp_up_behaviour
  {
    [Fact]
    public async Task constant()
    {
      var steps =
        await RunMemoryLoadTest(requestsPerSecond: 21, rampUpTo: 21, duration: TimeSpan.FromSeconds(2));

      steps.Count.ShouldBe(2);

      var request1 = JsonConvert.DeserializeObject<LambdaRequest>(steps[0][0].Message);
      request1.Duration.ShouldBe(TimeSpan.FromSeconds(1));
      request1.Journeys.Length.ShouldBe(1);
      request1.RequestCount.ShouldBe(12);

      var request2 = JsonConvert.DeserializeObject<LambdaRequest>(steps[0][1].Message);
      request2.Duration.ShouldBe(TimeSpan.FromSeconds(1));
      request2.Journeys.Length.ShouldBe(1);
      request2.RequestCount.ShouldBe(9);
    }

    static int RequestCountForStep(List<PublishRequest> requests)
    {
      return requests
        .Select(request => JsonConvert.DeserializeObject<LambdaRequest>(request.Message).RequestCount)
        .Sum();
    }

    [Fact]
    public async Task up()
    {
      var steps =
        await RunMemoryLoadTest(requestsPerSecond: 1, rampUpTo: 5, duration: TimeSpan.FromSeconds(5));


      steps.Count.ShouldBe(5);
      RequestCountForStep(steps[0]).ShouldBe(1);
      RequestCountForStep(steps[1]).ShouldBe(2);
      RequestCountForStep(steps[2]).ShouldBe(3);
      RequestCountForStep(steps[3]).ShouldBe(4);
      RequestCountForStep(steps[4]).ShouldBe(5);

      var request1 = JsonConvert.DeserializeObject<LambdaRequest>(steps[0][0].Message);
      request1.Duration.ShouldBe(TimeSpan.FromSeconds(1));
      request1.Journeys.Length.ShouldBe(1);
      request1.RequestCount.ShouldBe(1);
    }

    [Fact]
    public async Task down()
    {
      var steps =
        await RunMemoryLoadTest(requestsPerSecond: 50, rampUpTo: 10, duration: TimeSpan.FromSeconds(5));


      steps.Count.ShouldBe(5);
      RequestCountForStep(steps[0]).ShouldBe(50);
      RequestCountForStep(steps[1]).ShouldBe(40);
      RequestCountForStep(steps[2]).ShouldBe(30);
      RequestCountForStep(steps[3]).ShouldBe(20);
      RequestCountForStep(steps[4]).ShouldBe(10);

      var request1 = JsonConvert.DeserializeObject<LambdaRequest>(steps[0][0].Message);
      request1.Duration.ShouldBe(TimeSpan.FromSeconds(1));
      request1.Journeys.Length.ShouldBe(1);
      request1.RequestCount.ShouldBe(12);
    }

    static async Task<List<List<PublishRequest>>> RunMemoryLoadTest(int requestsPerSecond, int rampUpTo,
      TimeSpan duration)
    {
      var publishedRequests = new List<List<PublishRequest>>();
      var loadTest = new LoadTest(
        requestsPerSecond,
        rampUpTo,
        duration,
        async count => new[] {new MemoryJourney()},
        new LambdaControlPlane("topic1", 600, (requests, token) =>
        {
          publishedRequests.Add(requests.ToList());
          return Task.CompletedTask;
        }));

      await loadTest.RunAsync(CancellationToken.None);
      return publishedRequests;
    }
  }
}