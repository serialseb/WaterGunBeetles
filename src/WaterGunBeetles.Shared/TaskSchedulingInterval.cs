using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WaterGunBeetles
{
  public class TaskSchedulingInterval
  {
    Stopwatch _timer = new Stopwatch();
    TimeSpan _accumulatedDelay = TimeSpan.Zero;

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