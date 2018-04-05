using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Newtonsoft.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace WaterGunBeetles.Lambda
{
  public class AwsLambdaFunction<TJourney, TJourneyResult>
  {
    public delegate Task<TJourneyResult> JourneyTaker(TJourney journey);

    readonly JourneyTaker _journeyTaker;

    public AwsLambdaFunction(JourneyTaker journeyTaker)
    {
      _journeyTaker = journeyTaker;
    }

    public async Task Handler(SNSEvent snsEvent, ILambdaContext context)
    {
      var command = JsonConvert.DeserializeObject<AwsLambdaRequest<TJourney>>(snsEvent.Records[0].Sns.Message);

      var journeyReporter = new CloudWatchLogReporter<TJourney, TJourneyResult>(context.Logger);

      var executionInterval = command.Duration / command.RequestCount;

      var scheduler = new TaskSchedulingInterval();
      for (var i = 0; i < command.RequestCount; i++)
      {
        scheduler.Start();
        FireAndForgetJourneyWithLowMemoryStateDelegste(_journeyTaker, command, journeyReporter, i);

        await scheduler.WaitFor(executionInterval);
      }

      journeyReporter.ReportCompleted();
    }

    void FireAndForgetJourneyWithLowMemoryStateDelegste(JourneyTaker journeyTaker, AwsLambdaRequest<TJourney> command,
      CloudWatchLogReporter<TJourney, TJourneyResult> cloudWatchLogReporter, int journeyIndex)
    {
      Task.Factory.StartNew(
        iteration => InvokeJourney(
          journeyTaker,
          command.Journeys[(int) iteration % command.Journeys.Length],
          cloudWatchLogReporter),
        journeyIndex);
    }

    async Task InvokeJourney(JourneyTaker journeyTaker,
      TJourney journey,
      CloudWatchLogReporter<TJourney, TJourneyResult> cloudWatchLogReporter)
    {
      var sw = Stopwatch.StartNew();
      try
      {
        var result = await journeyTaker(journey);
        sw.Stop();
        cloudWatchLogReporter.ReportSuccess(journey, sw.Elapsed, result);
      }
      catch (Exception e)
      {
        cloudWatchLogReporter.ReportError(journey, sw.Elapsed, e);
      }
    }
  }
}