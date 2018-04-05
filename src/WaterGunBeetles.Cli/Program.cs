using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;
using CommandLine;
using WaterGunBeetles.Aws;

namespace WaterGunBeetles.Cli
{
  class Program
  {
    static async Task<int> Main(string[] args)
    {
      return await
        Parser.Default
          .ParseArguments<SquirtOptions>(args)
          .MapResult(async options => await SquirtAsync(options), errs => Task.FromResult(1));
    }

    static async Task<int> SquirtAsync(SquirtOptions options)
    {
      var ctrlC = new CancellationTokenSource();
      Console.CancelKeyPress += (sender, args) =>
      {
        Console.WriteLine("Beetles, abort mission!");
        ctrlC.Cancel();
      };
      
      var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
      var packagePath = options.PackagePath ?? GetDefaultPackagePath();
      var deployer = new LambdaDeployer(
        timestamp: timestamp.ToString(),
        packagePath: packagePath);
      
      var rps = options.RequestsPerSecond;
      var to = options.RampUpTo ?? rps;
      var duration = ParseDuration(options.Duration);
      Console.WriteLine($"Load testing from {rps}rps to {to}rps for {duration} using lambda {packagePath}");
      try
      {
        Console.WriteLine("Beetles are assembling...");
        await deployer.Deploy(10, options.MemorySize);
        Console.WriteLine("Beetles, attack!");

        var detailsLog = options.Verbose ? (Action<object>)WriteDetail : null;
        var lambdaControlPlane = new LambdaControlPlane(deployer.Topics.ToArray(), detailsLog: detailsLog);
        
        var nullLoadTest = new LoadTest<NullJourney>(
          rps,
          to,
          duration,
          new NullJourneyScript(),
          lambdaControlPlane,
          PrintLoadStep);
        var result = await nullLoadTest.RunAsync(ctrlC.Token);
        Console.WriteLine($"Beetles have returned in {result.Elapsed}.");
      }
      finally
      {
        Console.WriteLine("Calling back the beetles...");
        await deployer.Shutdown();
      }

      Console.WriteLine("It's goodbye from them, and it's goodbye from me.");
      return 0;
    }

    static void WriteDetail(object obj)
    {
      var prev = Console.ForegroundColor;
      Console.ForegroundColor = ConsoleColor.DarkGray;
      Console.WriteLine(obj);
      Console.ForegroundColor = prev;
    }

    static TimeSpan ParseDuration(string duration)
    {
      if (duration.EndsWith("s"))
        return TimeSpan.FromSeconds(Convert.ToDouble(duration.Substring(0, duration.Length - 1)));
      if (duration.EndsWith("m"))
        return TimeSpan.FromMinutes(Convert.ToDouble(duration.Substring(0, duration.Length - 1)));
      if (duration.EndsWith("h"))
        return TimeSpan.FromHours(Convert.ToDouble(duration.Substring(0, duration.Length - 1)));

      return TimeSpan.Parse(duration);
    }

    static void PrintLoadStep(LoadTestStepContext<NullJourney> obj)
    {
      Console.WriteLine($"[{obj.ExecutionTime.Elapsed}] Beetles in flight at {obj.RequestsPerSecond}rps");
    }

    static string GetDefaultPackagePath()
    {
      var proj = new DirectoryInfo(Directory.GetCurrentDirectory()).Name;
      var filePath = Path.Combine(Environment.CurrentDirectory, "bin", "Release", "netcoreapp2.0", proj) + ".zip";
      if (!File.Exists(filePath))
      {
        throw new ArgumentException($"Could not find a package at {filePath}.");
      }

      return filePath;
    }
  }
}