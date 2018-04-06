using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WaterGunBeetles.Server.Aws;

[assembly: TypeForwardedTo(typeof(LambdaFunction))]

namespace WaterGunBeetles.ConsoleTemplate.CSharp
{
  public class LoadTest : IBeetlesConfiguration
  {
    public void Configure(IBeetlesOptions options)
    {
      var storyTeller = new StoryTeller();
      var journeyWalker = new JourneyWalker();
      options
        .StoryTeller(count => storyTeller.CreateJourneys(count))
        .OnStoryStart(()=> storyTeller.Initialize())
        .JourneyWalker(journeyWalker.WalkJourney);
    }
  }
}