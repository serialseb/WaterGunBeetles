using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WaterGunBeetles.Cli
{
  class NullJourneyScript : IJourneyScript<NullJourney>
  {
    public Task Initialize()
    {
      return Task.CompletedTask;
    }

    public IEnumerable<NullJourney> CreateJourneys(int iterationCount)
    {
      return Enumerable.Repeat(new NullJourney(),iterationCount);
    }
  }
}