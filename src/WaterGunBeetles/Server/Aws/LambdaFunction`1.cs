using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Newtonsoft.Json;
using WaterGunBeetles.Internal;

namespace WaterGunBeetles.Server.Aws
{
  public class LambdaFunction<TJourney, TJourneyResult> : ILambdaFunction
  {
    readonly Func<TJourney, Task<TJourneyResult>> _journeyTaker;

    public LambdaFunction(BeetlesMetaModel model)
    {
      _journeyTaker = (Func<TJourney, Task<TJourneyResult>>) model.JourneyTaker;
    }

    public async Task Handle(SNSEvent snsEvent, ILambdaContext context)
    {
      var command = JsonConvert.DeserializeObject<LambdaRequest<TJourney>>(snsEvent.Records[0].Sns.Message);

      var journeyReporter = new CloudWatchLogReporter<TJourney, TJourneyResult>(context.Logger);

      var executionInterval = command.Duration / command.RequestCount;

      var scheduler = new TaskSchedulingInterval();
      
      var endOfStep = new CancellationTokenSource();
      var endOfStepDuration = command.Duration + executionInterval;
      endOfStep.CancelAfter(endOfStepDuration);
      var endOfStepTask = Task.Delay(endOfStepDuration + executionInterval, endOfStep.Token);
      
      var pendingTasks = new List<Task>();
      for (var i = 0; i < command.RequestCount; i++)
      {
        scheduler.Start();
        pendingTasks.Add(FireAndForgetJourneyWithLowMemoryStateDelegste(_journeyTaker, command, journeyReporter, i, endOfStep.Token));

        await scheduler.WaitFor(executionInterval, endOfStep.Token);
      }

      await Task.WhenAny(Task.WhenAll(pendingTasks), endOfStepTask);
      journeyReporter.ReportCompleted();
    }

    static Task FireAndForgetJourneyWithLowMemoryStateDelegste(Func<TJourney, Task<TJourneyResult>> journeyTaker,
      LambdaRequest<TJourney> command,
      CloudWatchLogReporter<TJourney, TJourneyResult> cloudWatchLogReporter,
      int journeyIndex,
      CancellationToken token)
    {
      return Task.Factory.StartNew(
        iteration => InvokeJourney(
          journeyTaker,
          command.Journeys[(int) iteration % command.Journeys.Length],
          cloudWatchLogReporter),
        journeyIndex,
        token).Unwrap();
    }

    static async Task InvokeJourney(Func<TJourney, Task<TJourneyResult>> journeyTaker,
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