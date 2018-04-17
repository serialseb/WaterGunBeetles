using System;
using System.Collections.Generic;

namespace WaterGunBeetles.Client
{
  public class LinearRampingStrategy
  {
    readonly int _from;
    readonly int _to;
    readonly TimeSpan _stepDuration;
    readonly double _stepRpsIncrease;
    readonly int _steps;

    public LinearRampingStrategy(int from, TimeSpan totalDuration, int to = -1)
    {
      if (to == -1) to = from;
      if (totalDuration > TimeSpan.FromMinutes(1))
      {
        _stepDuration = TimeSpan.FromMinutes(1);
        _steps = (int) totalDuration.TotalMinutes;
      }
      else
      {
        _stepDuration = TimeSpan.FromSeconds(1);
        _steps = (int) totalDuration.TotalSeconds;
      }
      _from = from;
      _to = to;
      var delta = to - from;

      _stepRpsIncrease = _steps != 0 ? ((double) delta / (_steps - 1)) : 0;
    }

    public IEnumerable<(int requestsPerSecond, TimeSpan waitFor)> GetSteps()
    {
      for (var step = 0; step < _steps; step++)
      {
        var stepRps = (int)Math.Round(_from + step * _stepRpsIncrease);
        if (GoingUp)
          stepRps = Math.Min(_to, stepRps);
        else if (GoingDown)
          stepRps = Math.Max(_to, stepRps);
        yield return (stepRps, _stepDuration);
      }
    }

    public bool GoingUp => _from < _to;
    public bool GoingDown => _from >= _to;
  }
}