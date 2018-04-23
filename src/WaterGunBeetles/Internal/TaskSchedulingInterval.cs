using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace WaterGunBeetles.Internal
{
  public class TaskSchedulingInterval
  {
    TimeSpan _accumulatedDelay = TimeSpan.Zero;
    readonly Stopwatch _timer = new Stopwatch();

    public void Start()
    {
      _timer.Restart();
    }

    public async Task WaitFor(TimeSpan delay, CancellationToken cancellationToken = default)
    {
      _timer.Stop();

      var timeLeft = delay - _timer.Elapsed + _accumulatedDelay;

      if (timeLeft > TimeSpan.Zero)
      {
        _accumulatedDelay = TimeSpan.Zero;
        await Task.Delay(timeLeft, cancellationToken);
      }
      else
      {
        _accumulatedDelay = timeLeft;
      }
    }
  } 
}