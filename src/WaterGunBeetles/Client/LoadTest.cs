using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;
using WaterGunBeetles.Internal;

namespace WaterGunBeetles.Client
{
  public class LoadTest
  {
    readonly IControlPlane _controlPlane;
    readonly Action<LoadTestStepContext> _onStep;
    readonly Func<IEnumerable<PublishRequest>, CancellationToken, Task> _publisher;
    readonly LinearRampingStrategy _rampStategy;
    readonly Func<int, Task<object[]>> _storyTeller;
    LinearRampingStrategy _runStrategy;

    public LoadTest(
      int requestsPerSecond,
      TimeSpan duration,
      TimeSpan rampTime,
      Func<int, Task<object[]>> storyTeller,
      IControlPlane controlPlane,
      Action<LoadTestStepContext> onStep = null)
    {
      _storyTeller = storyTeller;
      _controlPlane = controlPlane;
      _onStep = onStep ?? (_ => { });
      _publisher = controlPlane.Publisher;
      
      var stepRps = (int)Math.Round(requestsPerSecond / (rampTime.TotalMinutes > 1 ? rampTime.TotalMinutes :  rampTime.TotalSeconds));
      _rampStategy = new LinearRampingStrategy(stepRps, rampTime, requestsPerSecond);
      _runStrategy = new LinearRampingStrategy(requestsPerSecond, duration);

    }

    public async Task<LoadTestResult> RunAsync(CancellationToken token = default)
    {
      var ctx = new LoadTestStepContext
      {
        ExecutionTime = Stopwatch.StartNew(),
        Cancel = token,
        LoadTestId = Guid.NewGuid(),
        StoryTeller = _storyTeller,
        PublishAsync = _publisher
      };

      await RunSteps(token, _rampStategy.GetSteps(), ctx);
      await RunSteps(token, _runStrategy.GetSteps(), ctx);

      ctx.ExecutionTime.Stop();
      return new LoadTestResult(ctx.ExecutionTime.Elapsed);
    }

    async Task RunSteps(CancellationToken token, IEnumerable<(int requestsPerSecond, TimeSpan waitFor)> steps, LoadTestStepContext ctx)
    {
      var schedulingInterval = new TaskSchedulingInterval();

      foreach (var step in steps)
      {
        schedulingInterval.Start();
        ctx.RequestsPerSecond = step.requestsPerSecond;
        ctx.Duration = step.waitFor;

        await SetLoad(ctx);

        await schedulingInterval.WaitFor(step.waitFor, token);
      }
    }

    async Task SetLoad(LoadTestStepContext ctx)
    {
      _onStep(ctx);
      await _controlPlane.SetLoad(ctx);
    }
  }
}