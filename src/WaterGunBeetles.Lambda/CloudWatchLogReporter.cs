using System;
using Amazon.Lambda.Core;

namespace WaterGunBeetles.Lambda
{
  public class CloudWatchLogReporter<TJourney,TJourneyResult>
  {
    readonly ILambdaLogger _log;
    int _successCount;
    int _errorCount;
    TimeSpan _journeyTime = TimeSpan.Zero;

    public CloudWatchLogReporter(ILambdaLogger log)
    {
      _log = log;
    }


    public void ReportSuccess(TJourney journey, TimeSpan duration, TJourneyResult result)
    {
      _successCount++;
      _journeyTime += duration;
    }

    public void ReportError(TJourney journey, TimeSpan duration, Exception exception)
    {
      _errorCount++;
      _log.Log("[ERROR]" + exception);
      _journeyTime += duration;
    }

    public void ReportCompleted()
    {
      _log.Log(
        $"[INFO] Completed {_errorCount + _successCount} journeys in {_journeyTime} (Success: {_successCount}, Errors: {_errorCount})");
    }
  }
}