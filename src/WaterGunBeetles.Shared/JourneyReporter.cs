using System;

namespace WaterGunBeetles
{
  public class JourneyReporter<TJourney>
  {
    public void ReportSuccess(TJourney journey, TimeSpan swElapsed) { }

    public void ReportError(TJourney journey, TimeSpan swElapsed, Exception exception) { }
  }
}