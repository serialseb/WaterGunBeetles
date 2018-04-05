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
    readonly Func<IEnumerable<PublishRequest>, CancellationToken, Task> _publisher;
    readonly LinearRampingStrategy _stategy;

    public LoadTest(int requestsPerSecond,
      int rampUpTo,
      TimeSpan duration,
      IJourneyScript<TJourney> testScript,
      IControlPlane controlPlane)
    {
      _testScript = testScript;
      _controlPlane = controlPlane;
      _publisher = controlPlane.Publisher;
      _stategy = new LinearRampingStrategy(requestsPerSecond, rampUpTo, duration);
    }

    public async Task<LoadTestResult> RunAsync(CancellationToken token = default)
    {
      await _testScript.Initialize();
      var ctx = new LoadTestStepContext<TJourney>
      {
        Elapsed = Stopwatch.StartNew(),
        Cancel = token,
        LoadTestId = Guid.NewGuid(),
        TestScript = _testScript,
        PublishAsync = _publisher
      };

      var stepTime = new Stopwatch();
      foreach (var step in _stategy.GetSteps())
      {
        stepTime.Reset();
        ctx.RequestsPerSecond = step.requestsPerSecond;
        ctx.Duration = step.waitFor;

        await _controlPlane.SetLoad(ctx);

        var next = step.waitFor - stepTime.Elapsed;
        if (next > TimeSpan.Zero)
          await Task.Delay(next, token);
        if (token.IsCancellationRequested)
          break;
      }


      ctx.Elapsed.Stop();

      return new LoadTestResult();
    }

    public Task Close()
    {
      return Task.CompletedTask;
    }
  }
}