using System;
using System.Diagnostics;
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

    public async Task WaitFor(TimeSpan delay)
    {
      _timer.Stop();

      var timeLeft = delay - _timer.Elapsed + _accumulatedDelay;

      if (timeLeft > TimeSpan.Zero)
      {
        _accumulatedDelay = TimeSpan.Zero;
        await Task.Delay(timeLeft);
      }
      else
      {
        _accumulatedDelay = timeLeft;
      }
    }
  }
}