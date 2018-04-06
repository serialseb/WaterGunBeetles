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
    readonly Func<int, Task<object[]>> _storyTeller;
    readonly IControlPlane _controlPlane;
    readonly Action<LoadTestStepContext> _onStep;
    readonly Func<IEnumerable<PublishRequest>, CancellationToken, Task> _publisher;
    readonly LinearRampingStrategy _stategy;

    public LoadTest(
      int requestsPerSecond,
      int rampUpTo,
      TimeSpan duration,
      Func<int, Task<object[]>> storyTeller,
      IControlPlane controlPlane,
      Action<LoadTestStepContext> onStep = null)
    {
      _storyTeller = storyTeller;
      _controlPlane = controlPlane;
      _onStep = onStep ?? (_=>{});
      _publisher = controlPlane.Publisher;
      _stategy = new LinearRampingStrategy(requestsPerSecond, rampUpTo, duration);
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

    async Task SetLoad(LoadTestStepContext ctx)
    {
      _onStep(ctx);
      await _controlPlane.SetLoad(ctx);
    }
  }
}