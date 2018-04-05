using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;

namespace WaterGunBeetles
{
  public class LoadTestStepContext<TJourney>
  {
    public Stopwatch Elapsed { get; set; }
    public CancellationToken Cancel { get; set; }
    public int RequestsPerSecond { get; set; }
    public TimeSpan Duration { get; set; }
    public Guid LoadTestId { get; set; }
    public IJourneyScript<TJourney> TestScript { get; set; }
    public Func<IEnumerable<PublishRequest>, CancellationToken, Task> PublishAsync { get; set; }
  }
}