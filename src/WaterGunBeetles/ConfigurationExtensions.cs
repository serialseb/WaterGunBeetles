using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WaterGunBeetles.Internal;

namespace WaterGunBeetles
{
  public static class ConfigurationExtensions
  {
    public static IBeetlesJourney<TJourney> StoryTeller<TJourney>(this IBeetlesOptions options,
      Func<int, Task<IEnumerable<TJourney>>> storyTeller)
    {
      options.MetaModel.StoryTeller = async count => (await storyTeller.Invoke(count)).Cast<object>().ToArray();
      options.MetaModel.JourneyType = typeof(TJourney);
      return new BeetlesJourneyOptions<TJourney>(options);
    }

    public static IBeetlesJourney<TJourney> JourneyWalker<TJourney, TJourneyResult>(
      this IBeetlesJourney<TJourney> options,
      Func<TJourney,Task<TJourneyResult>> journeyTaker)
    {
      options.MetaModel.JourneyTaker = journeyTaker;
      options.MetaModel.JourneyTypeResult = typeof(TJourneyResult);
      return options;
    }

    public static T OnStoryStart<T>(this T options, Func<Task> onStoryStart) where T : IBeetlesOptions
    {
      options.MetaModel.OnStoryStart = onStoryStart; 
      return options;
    }
  }
}