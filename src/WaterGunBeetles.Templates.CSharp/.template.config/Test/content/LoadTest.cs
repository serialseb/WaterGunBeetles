using WaterGunBeetles;

namespace WaterGunBeetles.Templates.CSharp
{
  public class LoadTest : IBeetlesConfiguration
  {
    public void Configure(IBeetlesOptions options)
    {
      var storyTeller = new StoryTeller();
      var journeyWalker = new JourneyWalker();
      options
        .StoryTeller(count => storyTeller.CreateJourneys(count))
        .OnStoryStart(() => storyTeller.Initialize())
        .JourneyWalker(journeyWalker.WalkJourney);
    }
  }
}