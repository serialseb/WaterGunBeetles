using System.Collections.Generic;
using System.Threading.Tasks;

namespace WaterGunBeetles
{
  public interface IJourneyScript<TJourney>
  {
    Task Initialize();

    IEnumerable<TJourney> CreateJourneys(int iterationCount);
  }
}