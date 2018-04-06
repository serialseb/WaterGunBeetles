using System;

namespace WaterGunBeetles.Client.Aws
{
  public class LambdaRequest
  {
    public object[] Journeys { get; set; }
    public TimeSpan Duration { get; set; }
    public int RequestCount { get; set; }
  }
}