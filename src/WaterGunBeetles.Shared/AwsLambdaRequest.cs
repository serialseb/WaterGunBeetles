using System;

namespace WaterGunBeetles
{
  public class AwsLambdaRequest<TJourney>
  {
    public TJourney[] Journeys { get; set; }
    public TimeSpan Duration { get; set; }
    public int RequestCount { get; set; }
  }
}
