using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WaterGunBeetles.Internal
{
  public class BeetlesMetaModel
  {
    public Func<int, Task<object[]>> StoryTeller { get; set; }
    IDictionary<string, object> Extensions { get; } = new Dictionary<string, object>();
    public Type JourneyType { get; set; }
    public Type JourneyTypeResult { get; set; }

    public object JourneyTaker { get; set; }
    public Func<Task> OnStoryStart { get; set; }
  }
}