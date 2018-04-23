using System;
using Amazon.Lambda.Core;

namespace WaterGunBeetles.Server.Aws
{
  public class CloudWatchLogReporter<TJourney, TJourneyResult>
  {
    readonly ILambdaLogger _log;
    int _errorCount;
    TimeSpan _journeyTime = TimeSpan.Zero;
    int _successCount;

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

    public void ReportFinished(CompletionState state, Exception error = null)
    {
      if (error != null)
      {
        _log.Log(
          $"[ERROR] {state} - Completed {_errorCount + _successCount} journeys in {_journeyTime}" +
          $" (Success: {_successCount}, Errors: {_errorCount}){Environment.NewLine}" +
          $"{error}");
      }
      else
      {
        _log.Log(
        $"[INFO] {state} - Completed {_errorCount + _successCount} journeys in {_journeyTime} " +
        $"(Success: {_successCount}, Errors: {_errorCount})");
      }
    }
  }

  public enum CompletionState
  {
    Success,
    Error,
    Timeout
  }
}