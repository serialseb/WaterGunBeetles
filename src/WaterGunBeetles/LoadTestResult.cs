using System;
using System.Diagnostics;

namespace WaterGunBeetles
{
  public class LoadTestResult {
    public TimeSpan Elapsed { get; }

    public LoadTestResult(TimeSpan elapsed)
    {
      Elapsed = elapsed;
    }
  }
}
