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
        await RunMemoryLoadTest(requestsPerSecond: 21, duration: TimeSpan.FromSeconds(2));

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
    public async Task warmup()
    {
      var steps =
        await RunMemoryLoadTest(requestsPerSecond: 50, duration: TimeSpan.Zero,
          rampUpDuration: TimeSpan.FromSeconds(5));


      steps.Count.ShouldBe(5);
      RequestCountForStep(steps[0]).ShouldBe(10);
      RequestCountForStep(steps[1]).ShouldBe(20);
      RequestCountForStep(steps[2]).ShouldBe(30);
      RequestCountForStep(steps[3]).ShouldBe(40);
      RequestCountForStep(steps[4]).ShouldBe(50);

      var request1 = JsonConvert.DeserializeObject<LambdaRequest>(steps[0][0].Message);
      request1.Duration.ShouldBe(TimeSpan.FromSeconds(1));
      request1.Journeys.Length.ShouldBe(1);
      request1.RequestCount.ShouldBe(10);
    }

    [Fact]
    public async Task warmup_then_run()
    {
      var steps =
        await RunMemoryLoadTest(requestsPerSecond: 50, duration: TimeSpan.FromSeconds(5),
          rampUpDuration: TimeSpan.FromSeconds(5));


      steps.Count.ShouldBe(10);
      RequestCountForStep(steps[0]).ShouldBe(10);
      RequestCountForStep(steps[1]).ShouldBe(20);
      RequestCountForStep(steps[2]).ShouldBe(30);
      RequestCountForStep(steps[3]).ShouldBe(40);
      RequestCountForStep(steps[4]).ShouldBe(50);

      RequestCountForStep(steps[5]).ShouldBe(50);
      RequestCountForStep(steps[6]).ShouldBe(50);
      RequestCountForStep(steps[7]).ShouldBe(50);
      RequestCountForStep(steps[8]).ShouldBe(50);
      RequestCountForStep(steps[9]).ShouldBe(50);
      
      var request1 = JsonConvert.DeserializeObject<LambdaRequest>(steps[0][0].Message);
      request1.Duration.ShouldBe(TimeSpan.FromSeconds(1));
      request1.Journeys.Length.ShouldBe(1);
      request1.RequestCount.ShouldBe(10);
    }

    static async Task<List<List<PublishRequest>>> RunMemoryLoadTest(int requestsPerSecond,
      TimeSpan duration,
      TimeSpan rampUpDuration = default)
    {
      var publishedRequests = new List<List<PublishRequest>>();
      var loadTest = new LoadTest(
        requestsPerSecond,
        duration,
        rampUpDuration,
        async count => new[] {new MemoryJourney()}, new LambdaControlPlane("topic1", 600, (requests, d, token) =>
        {
          publishedRequests.Add(requests.ToList());
          return Task.CompletedTask;
        }));

      await loadTest.RunAsync(CancellationToken.None);
      return publishedRequests;
    }
  }

  public class linear_rampup_calculations
  {
    [Fact]
    public void up()
    {
      var steps = new LinearRampingStrategy(10, TimeSpan.FromMinutes(10), 50).GetSteps().ToList();

      steps.Count.ShouldBe(10);
      steps[0].requestsPerSecond.ShouldBe(10);
      steps[9].requestsPerSecond.ShouldBe(50);
    }

    [Fact]
    public void constant()
    {
      var steps = new LinearRampingStrategy(50, TimeSpan.FromMinutes(10)).GetSteps().ToList();

      steps.Count.ShouldBe(10);
      steps[0].requestsPerSecond.ShouldBe(50);
      steps[9].requestsPerSecond.ShouldBe(50);
    }

    [Fact]
    public void zero()
    {
      var steps = new LinearRampingStrategy(50, TimeSpan.FromMinutes(10)).GetSteps().ToList();

      steps.Count.ShouldBe(10);
      steps[0].requestsPerSecond.ShouldBe(50);
      steps[9].requestsPerSecond.ShouldBe(50);
    }
  }
}