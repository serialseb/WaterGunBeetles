using System;

namespace WaterGunBeetles.Client
{
  public class LoadTestResult
  {
    public LoadTestResult(TimeSpan elapsed)
    {
      Elapsed = elapsed;
    }

    public TimeSpan Elapsed { get; }
  }
}