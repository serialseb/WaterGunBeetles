using System;

namespace WaterGunBeetles.Server.Aws
{
  public class LambdaRequest<T>
  {
    public T[] Journeys { get; set; }
    public TimeSpan Duration { get; set; }
    public int RequestCount { get; set; }
  }
}