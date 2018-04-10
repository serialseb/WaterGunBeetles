using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using WaterGunBeetles.Client;
using WaterGunBeetles.Client.Aws;
using WaterGunBeetles.Internal;

namespace WaterGunBeetles.Cli
{
  static class Program
  {
    static AssemblyInformationalVersionAttribute _version = (AssemblyInformationalVersionAttribute) Attribute.GetCustomAttribute(typeof(Program).Assembly,
      typeof(AssemblyInformationalVersionAttribute));

    static ShellAssemblyLoadContext _shellAsm;

    static async Task<int> Main(string[] args)
    {
      Console.WriteLine($"💦🔫🐞 v{_version.InformationalVersion}");
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
        Console.WriteLine("Beetles, abort!");
        ctrlC.Cancel();
      };

      var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
      var packagePath = options.PackagePath ?? GetDefaultPackagePath(options.Configuration, options.Framework);

      var settings = LoadTypeInfo(options.Configuration, options.Framework);
      var deployer = new LambdaDeployer(
        timestamp: timestamp.ToString(),
        packagePath: packagePath,
        settingsType: settings.settingsType);

      var rps = options.RequestsPerSecond;
      var to = options.RampUpTo ?? rps;
      var duration = ParseDuration(options.Duration);
      Console.WriteLine($"Load testing from {rps}rps to {to}rps for {duration} using lambda {packagePath}");
      try
      {
        Console.WriteLine("Beetles, assemble!");
        await deployer.Deploy(10, options.MemorySize);
        Console.WriteLine("Beetles, attack!");

        var detailsLog = options.Verbose ? (Action<object>) WriteDetail : null;
        var lambdaControlPlane = new LambdaControlPlane(deployer.Topics.ToArray(), detailsLog: detailsLog);

        var nullLoadTest = new LoadTest(
          rps,
          to,
          duration,
          settings.model.StoryTeller,
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

    static (BeetlesMetaModel model, Type settingsType) LoadTypeInfo(
      string buildConfiguration, string buildFramework)
    {
      var proj = new DirectoryInfo(Directory.GetCurrentDirectory()).Name;
      var assemblyDir = Path.Combine(Environment.CurrentDirectory, "bin", buildConfiguration, buildFramework);
      var assemblyPath = Path.Combine(assemblyDir, proj + ".dll");
      if (File.Exists(assemblyPath) == false)
        throw new ArgumentException($"Could not find an assembly at {assemblyPath}");
      
      _shellAsm = new ShellAssemblyLoadContext(assemblyDir);
      var assembly = _shellAsm.LoadFromAssemblyPath(assemblyPath);
      
      var settings = MetaModelFactory.FromAssembly(assembly);
      return settings;
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

    static void PrintLoadStep(LoadTestStepContext obj)
    {
      Console.WriteLine($"[{obj.ExecutionTime.Elapsed}] Beetles in flight at {obj.RequestsPerSecond}rps");
    }

    static string GetDefaultPackagePath(string configuration, string framework)
    {
      var proj = new DirectoryInfo(Directory.GetCurrentDirectory()).Name;
      var filePath = Path.Combine(Environment.CurrentDirectory, "bin", configuration, framework, proj) + ".zip";
      if (!File.Exists(filePath))
      {
        throw new ArgumentException($"Could not find a package at {filePath}. Did you call 'dotnet lambda package'?");
      }

      return filePath;
    }
  }

  class ShellAssemblyLoadContext : AssemblyLoadContext
  {
    Dictionary<AssemblyName, Assembly> _loadedAsm;

    public ShellAssemblyLoadContext(string basePath)
    {
      _loadedAsm = Directory.GetFiles(basePath, "*.dll")
        .Select(TryLoadAssemblyName)
        .Where(a => a != null)
        .ToDictionary(a => a.GetName());
    }

    Assembly TryLoadAssemblyName(string path)
    {
      try
      {
        return Default.LoadFromAssemblyPath(path);

      }
      catch
      {
        return null;
      }
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
      return _loadedAsm.TryGetValue(assemblyName, out var asm) ? asm : Default.LoadFromAssemblyName(assemblyName);
    }
  }
}