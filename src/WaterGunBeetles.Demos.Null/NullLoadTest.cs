using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WaterGunBeetles.Server.Aws;

[assembly: TypeForwardedTo(typeof(LambdaFunction))]

namespace WaterGunBeetles.Demos.Null
{
  public class NullLoadTest : IBeetlesConfiguration
  {
    public void Configure(IBeetlesOptions options)
    {
      options
        .StoryTeller(count => Task.FromResult(Enumerable.Repeat(new NullJourney(), count)))
        .JourneyWalker(journey => Task.FromResult(new NullJourneyResult()));
    }
  }
}