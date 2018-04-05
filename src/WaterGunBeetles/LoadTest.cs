using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;

namespace WaterGunBeetles
{
  public class LoadTest<TJourney>
  {
    readonly IJourneyScript<TJourney> _testScript;
    readonly IControlPlane _controlPlane;
    readonly Action<LoadTestStepContext<TJourney>> _onStep;
    readonly Func<IEnumerable<PublishRequest>, CancellationToken, Task> _publisher;
    readonly LinearRampingStrategy _stategy;

    public LoadTest(
      int requestsPerSecond,
      int rampUpTo,
      TimeSpan duration,
      IJourneyScript<TJourney> testScript,
      IControlPlane controlPlane,
      Action<LoadTestStepContext<TJourney>> onStep = null)
    {
      _testScript = testScript;
      _controlPlane = controlPlane;
      _onStep = onStep ?? (_=>{});
      _publisher = controlPlane.Publisher;
      _stategy = new LinearRampingStrategy(requestsPerSecond, rampUpTo, duration);
    }

    public async Task<LoadTestResult> RunAsync(CancellationToken token = default)
    {
      await _testScript.Initialize();
      var ctx = new LoadTestStepContext<TJourney>
      {
        ExecutionTime = Stopwatch.StartNew(),
        Cancel = token,
        LoadTestId = Guid.NewGuid(),
        TestScript = _testScript,
        PublishAsync = _publisher
      };

      var schedulingInterval = new TaskSchedulingInterval();
      foreach (var step in _stategy.GetSteps())
      {
        schedulingInterval.Start();
        ctx.RequestsPerSecond = step.requestsPerSecond;
        ctx.Duration = step.waitFor;

        await SetLoad(ctx);

        await schedulingInterval.WaitFor(step.waitFor);
        if (token.IsCancellationRequested)
          break;
      }


      ctx.ExecutionTime.Stop();

      return new LoadTestResult(ctx.ExecutionTime.Elapsed);
    }

    async Task SetLoad(LoadTestStepContext<TJourney> ctx)
    {
      _onStep(ctx);
      await _controlPlane.SetLoad(ctx);
    }

    public Task Close()
    {
      return Task.CompletedTask;
    }
  }
}