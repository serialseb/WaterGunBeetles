using System;

namespace WaterGunBeetles.Client
{
  public class LoadTestResult {
    public TimeSpan Elapsed { get; }

    public LoadTestResult(TimeSpan elapsed)
    {
      Elapsed = elapsed;
    }
  }
}
