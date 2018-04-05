using System;
using System.IO;
using System.Threading.Tasks;
using CommandLine;

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
      var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
      var deployer = new LambdaDeployer(
        timestamp: timestamp.ToString(),
        packagePath: options.PackagePath ?? GetDefaultPackagePath());
      Console.WriteLine("Beetle fleets, assemble...");
      await deployer.Deploy(10, options.MemorySize);
      Console.WriteLine("Beetles, attack!");
      await deployer.Shutdown();
      return 0;
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

  [Verb("squirt", HelpText = "Start a load test.")]
  class SquirtOptions
  {
    [Option]
    public string PackagePath { get; set; }

    [Option('m', Default = 128, HelpText = "The memory size used for the lambda function")]
    public int MemorySize { get; set; }
  }
}