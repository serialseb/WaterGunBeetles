using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;

namespace WaterGunBeetles
{
  public interface IControlPlane
  {
    Task SetLoad<TJourney>(LoadTestStepContext<TJourney> ctx);
    Func<IEnumerable<PublishRequest>, CancellationToken, Task> Publisher { get; set; }
  }
}