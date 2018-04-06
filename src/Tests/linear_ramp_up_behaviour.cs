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
using WaterGunBeetles.Aws;
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
      steps[0].Count.ShouldBe(21);
      steps[1].Count.ShouldBe(21);

      var request1 = JsonConvert.DeserializeObject<AwsLambdaRequest>(steps[0][0].Message);
      request1.Duration.ShouldBe(TimeSpan.FromSeconds(1));
      request1.Journeys.Length.ShouldBe(1);
      request1.RequestCount.ShouldBe(1);
    }

    [Fact]
    public async Task up()
    {
      var steps =
        await RunMemoryLoadTest(requestsPerSecond: 1, rampUpTo: 5, duration: TimeSpan.FromSeconds(5));


      steps.Count.ShouldBe(5);
      steps[0].Count.ShouldBe(1);
      steps[1].Count.ShouldBe(2);
      steps[2].Count.ShouldBe(3);
      steps[3].Count.ShouldBe(4);
      steps[4].Count.ShouldBe(5);

      var request1 = JsonConvert.DeserializeObject<AwsLambdaRequest>(steps[0][0].Message);
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
      steps[0].Count.ShouldBe(50);
      steps[1].Count.ShouldBe(40);
      steps[2].Count.ShouldBe(30);
      steps[3].Count.ShouldBe(20);
      steps[4].Count.ShouldBe(10);

      var request1 = JsonConvert.DeserializeObject<AwsLambdaRequest>(steps[0][0].Message);
      request1.Duration.ShouldBe(TimeSpan.FromSeconds(1));
      request1.Journeys.Length.ShouldBe(1);
      request1.RequestCount.ShouldBe(1);
    }

    static async Task<List<List<PublishRequest>>> RunMemoryLoadTest(int requestsPerSecond, int rampUpTo,
      TimeSpan duration)
    {
      var publishedRequests = new List<List<PublishRequest>>();
      var storyTeller = new MemoryJourneyScript();
      var loadTest = new LoadTest(
        requestsPerSecond,
        rampUpTo,
        duration,
        count=> storyTeller.CreateJourneys(count),
        new LambdaControlPlane(new[] {"topic1"}, (requests, token) =>
        {
          publishedRequests.Add(requests.ToList());
          return Task.CompletedTask;
        }));

      await storyTeller.Initialize();
      await loadTest.RunAsync(CancellationToken.None);
      return publishedRequests;
    }
  }
}