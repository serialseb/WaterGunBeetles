using System;
using System.Collections.Generic;
using System.Linq;

namespace WaterGunBeetles.Client
{
  public static class JourneyCalcuations
  {
    public static List<int> JourneyCounts(
      int provisionedConcurrency,
      int requestsPerSecond,
      double totalSeconds,
      int minJourneysPerExecution)
    {
      // This is pretty shit, but i have a fever, so i'll fix it and apologise later on. It works. Sue me.
      var totalJourneys = (int) Math.Ceiling(requestsPerSecond * totalSeconds);

      var journeysPerExecution = totalJourneys / minJourneysPerExecution;
      var journeysLeftover = totalJourneys % minJourneysPerExecution;

      if (journeysPerExecution <= minJourneysPerExecution)
      {
        journeysPerExecution = minJourneysPerExecution;
      }
      else
      {
        journeysPerExecution = totalJourneys / provisionedConcurrency;
        journeysLeftover = totalJourneys % provisionedConcurrency;
      }

      var executions = totalJourneys / journeysPerExecution;

      var rounds = Enumerable.Range(0, executions).Select(pos => journeysPerExecution);
      if (journeysLeftover > 0)
        rounds = rounds.Concat(new[] {journeysLeftover});
      return rounds.ToList();
    }
  }
}