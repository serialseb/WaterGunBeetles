using System.Collections.Generic;
using System.Threading.Tasks;
using WaterGunBeetles;

namespace Tests.Infrastructure
{
  public class MemoryJourneyScript : IJourneyScript<MemoryJourney>
  {
    public Task Initialize()
    {
      return Task.CompletedTask;
    }

    public IEnumerable<MemoryJourney> CreateJourneys(int iterationCount)
    {
      return new[] {new MemoryJourney()};
    }
  }
}