using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;

namespace WaterGunBeetles.Client
{
  public class LoadTestStepContext
  {
    public Stopwatch ExecutionTime { get; set; }
    public CancellationToken Cancel { get; set; }
    public int RequestsPerSecond { get; set; }
    public TimeSpan Duration { get; set; }
    public Guid LoadTestId { get; set; }
    public Func<int, Task<object[]>> StoryTeller { get; set; }
    public Func<IEnumerable<PublishRequest>, TimeSpan, CancellationToken, Task> PublishAsync { get; set; }
  }
}