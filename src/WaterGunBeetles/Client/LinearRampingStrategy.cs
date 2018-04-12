using System;
using System.Collections.Generic;

namespace WaterGunBeetles.Client
{
  public class LinearRampingStrategy
  {
    readonly int _from;
    readonly TimeSpan _stepDuration;
    readonly int _stepRpsIncrease;
    readonly int _steps;

    public LinearRampingStrategy(int from, int to, TimeSpan totalDuration)
    {
      _from = from;
      var delta = to - from;

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

      _stepRpsIncrease = _steps > 1 ? delta / (_steps - 1) : 0;
    }

    public IEnumerable<(int requestsPerSecond, TimeSpan waitFor)> GetSteps()
    {
      for (var step = 0; step < _steps; step++)
        yield return (_from + step * _stepRpsIncrease, _stepDuration);
    }
  }
}