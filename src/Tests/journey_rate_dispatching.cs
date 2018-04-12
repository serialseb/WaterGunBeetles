using System.Linq;
using Shouldly;
using Xunit;
using WaterGunBeetles.Client;

namespace Tests
{
  public class journey_rate_dispatching
  {
    [Fact]
    public void less_than_min_journeys_execution()
    {
      // 1 request per second for 70 seconds, 70 requests, over max 100 invocation, min requests of 12 per minute
      // should do 60 / 12 = 5 journeys with 12 requests
      var result = JourneyCalcuations.JourneyCounts(100, 1, 60, 12);
      result.Sum().ShouldBe(60);
      result.ShouldBe(Enumerable.Repeat(12, 5));
    }

    [Fact]
    public void less_than_min_journeys_execution_with_leftovers()
    {
      // 1 request per second for 70 seconds, 60 requests, over max 100 invocation, min requests of 12 per minute
      // should do 70 / 12 = 5 journeys with 12 requests and 1 request with 10 journeys
      var result = JourneyCalcuations.JourneyCounts(100, 1, 70, 12);
      result.Sum().ShouldBe(70);
      result.ShouldBe(new[] {12, 12, 12, 12, 12, 10});
    }

    [Fact]
    public void double_provisioned_execution()
    {
      // we want double the number of 50 provisioned, thats 100 execution
      // 12 journeys pre execution, thats 1200 total journeys
      // over 60 seconds that's 20rps
      var result = JourneyCalcuations.JourneyCounts(50, 20, 60, 12);
      result.Sum().ShouldBe(1200);
      result.Count().ShouldBe(50);
    }
    
    [Fact]
    public void rounding()
    {
      // 5 rx/s for 60s = 300 requests
      // 300 / 12 = 25 executions
      
      var result = JourneyCalcuations.JourneyCounts(600, 5, 60, 12);
      result.Sum().ShouldBe(300);
      result.Count().ShouldBe(25);
    }
  }
}