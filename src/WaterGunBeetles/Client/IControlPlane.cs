using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;

namespace WaterGunBeetles.Client
{
  public interface IControlPlane
  {
    Func<IEnumerable<PublishRequest>, TimeSpan, CancellationToken, Task> Publisher { get; set; }
    Task SetLoad(LoadTestStepContext ctx);
  }
}