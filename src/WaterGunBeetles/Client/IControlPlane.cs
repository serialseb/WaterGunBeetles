using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;

namespace WaterGunBeetles.Client
{
  public interface IControlPlane
  {
    Task SetLoad(LoadTestStepContext ctx);
    Func<IEnumerable<PublishRequest>, CancellationToken, Task> Publisher { get; set; }
  }
}