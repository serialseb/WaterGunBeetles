using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Newtonsoft.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace WaterGunBeetles.Lambda
{
  public class NullFunction : AwsLambdaFunction<NullJourneyTaker,NullJourney>{}

  public class NullJourneyTaker : IJourneyTaker<NullJourney>
  {
    public Task TakeJourney<TJourney>(TJourney journey)
    {
      return Task.CompletedTask;
    }
  }

  public class NullJourney
  {
  }

  public class AwsLambdaFunction<T, TJourney>
    where T : IJourneyTaker<TJourney>, new()
  {
    public async Task Handler(SNSEvent snsEvent, ILambdaContext context)
    {
      var command = JsonConvert.DeserializeObject<AwsLambdaRequest<TJourney>>(snsEvent.Records[0].Sns.Message);

      var journeyTaker = new T();
      var journeyReporter = new JourneyReporter<TJourney>();

      var executionInterval = command.Duration / command.RequestCount;

      var taskSchedulingDelay = new Stopwatch();

      var accumulatedDelay = TimeSpan.Zero;
      for (var i = 0; i < command.RequestCount; i++)
      {
        taskSchedulingDelay.Restart();

        FireAndForgetJourneyWithLowMemoryStateDelegste(journeyTaker, command, journeyReporter, i);

        taskSchedulingDelay.Stop();
        var timeLeft = executionInterval - taskSchedulingDelay.Elapsed + accumulatedDelay;

        if (timeLeft > TimeSpan.Zero)
        {
          accumulatedDelay = TimeSpan.Zero;
          await Task.Delay(timeLeft);
        }
        else
        {
          accumulatedDelay = timeLeft;
        }
      }
    }

    void FireAndForgetJourneyWithLowMemoryStateDelegste(T journeyTaker, AwsLambdaRequest<TJourney> command,
      JourneyReporter<TJourney> journeyReporter, int journeyIndex)
    {
      Task.Factory.StartNew(
        iteration => InvokeJourney(
          journeyTaker,
          command.Journeys[(int) iteration % command.Journeys.Length],
          journeyReporter),
        journeyIndex);
    }

    async Task InvokeJourney(IJourneyTaker<TJourney> journeyTaker,
      TJourney journey,
      JourneyReporter<TJourney> journeyReporter)
    {
      var sw = Stopwatch.StartNew();
      try
      {
        await journeyTaker.TakeJourney(journey);
        sw.Stop();
        journeyReporter.ReportSuccess(journey, sw.Elapsed);
      }
      catch (Exception e)
      {
        journeyReporter.ReportError(journey, sw.Elapsed, e);
      }
    }
  }
}