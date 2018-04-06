using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WaterGunBeetles.Server.Aws;

[assembly: TypeForwardedTo(typeof(LambdaFunction))]

namespace WaterGunBeetles.Demos.Null
{
  public class Configuration : IBeetlesConfiguration
  {
    public void Configure(IBeetlesOptions options)
    {
      options
        .StoryTeller(async count => Enumerable.Repeat(new NullJourney(), count))
        .JourneyWalker(async journey => Task.FromResult(new NullJourneyResult()));
    }
  }
}