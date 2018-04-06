using System;

namespace WaterGunBeetles
{
  public class AwsLambdaRequest
  {
    public object[] Journeys { get; set; }
    public TimeSpan Duration { get; set; }
    public int RequestCount { get; set; }
  }
  public class AwsLambdaRequest<T>
  {
    public T[] Journeys { get; set; }
    public TimeSpan Duration { get; set; }
    public int RequestCount { get; set; }
  }
}
