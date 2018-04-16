using System;
using System.Threading;

namespace WaterGunBeetles.Client.Aws
{
  public class LambdaCreationOptions
  {
    public LambdaCreationOptions(int memorySize,
      string timestamp,
      string packagePath,
      string settingsTypeName,
      string lambdaHandlerName,
      string name,
      int provisionedConcurrency,
      Action<object> verbose,
      CancellationToken cancellationToken)
    {
      MemorySize = memorySize;
      Timestamp = timestamp;
      PackagePath = packagePath;
      SettingsTypeName = settingsTypeName;
      LambdaHandlerName = lambdaHandlerName;
      Name = name;
      ProvisionedConcurrency = provisionedConcurrency;
      Verbose = verbose;
      CancellationToken = cancellationToken;
    }

    public int MemorySize { get; }
    public string Timestamp { get; }
    public string PackagePath { get; }
    public string SettingsTypeName { get; }
    public string LambdaHandlerName { get; }
    public string Name { get; }
    public int ProvisionedConcurrency { get; }
    public Action<object> Verbose { get; }
    public CancellationToken CancellationToken { get; }
  }
}