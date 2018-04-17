using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    static readonly AssemblyInformationalVersionAttribute _version =
      (AssemblyInformationalVersionAttribute) Attribute.GetCustomAttribute(typeof(Program).Assembly,
        typeof(AssemblyInformationalVersionAttribute));

    static ShellAssemblyLoadContext _shellAsm;

    static async Task<int> Main(string[] args)
    {
      Console.WriteLine($"💦🔫🐞 v{_version.InformationalVersion}");

      return await
        CommandLine.Parser.Default
          .ParseArguments<SquirtOptions>(args)
          .MapResult(async options => await SquirtAsync(options), errs => Task.FromResult(1));
    }

    static async Task<int> SquirtAsync(SquirtOptions options)
    {
      options.Validate();
      var ctrlC = new CancellationTokenSource();
      Console.CancelKeyPress += (sender, args) =>
      {
        Console.WriteLine("Beetles, abort!");
        args.Cancel = true;
        ctrlC.Cancel();
      };
      var verbose = options.Verbose ? (Action<object>) WriteVerbose : o => { };

      var timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd_HHmmss");

      Console.WriteLine("Preparing battle plan...");
      var packagePath = options.PackagePath ?? GetDefaultPackagePath(options);

      var settings = LoadTypeInfo(options.Configuration, options.Framework, options.Name);

      var deployer = new LambdaDeployer(
        timestamp: timestamp,
        packagePath: packagePath,
        configurationType: settings.ConfigurationType,
        verbose: verbose);

      var rps = options.RequestsPerSecond;
      var duration = Parser.Duration(options.Duration);

      Console.WriteLine($"Load testing at {rps}rps for {duration} using lambda {packagePath}");

      try
      {
        Console.WriteLine("Beetles, assemble!");
        await deployer.Deploy(options.MemorySize, settings.Name, ctrlC.Token);

        Console.WriteLine("Beetles, attack!");

        var lambdaControlPlane = new LambdaControlPlane(deployer.Topic, 600, detailsLog: verbose);

        var nullLoadTest = new LoadTest(
          rps,
          duration,
          Parser.Duration(options.WarmUpTime),
          settings.StoryTeller,
          lambdaControlPlane,
          PrintLoadStep);

        var result = await nullLoadTest.RunAsync(ctrlC.Token);
        Console.WriteLine($"Beetles have returned in {result.Elapsed}.");
      }
      catch (TaskCanceledException)
      {
      }
      catch (Exception e)
      {
        WriteError($"An error occured - {e.Message}");
        
        WriteVerbose(e.ToString());
      }
      finally
      {
        Console.WriteLine("Calling back the beetles...");
        await deployer.Shutdown();
      }

      Console.WriteLine("It's goodbye from them, and it's goodbye from me!");
      return 0;
    }

    static BeetlesMetaModel LoadTypeInfo(string buildConfiguration,
      string buildFramework,
      string name)
    {
      var proj = new DirectoryInfo(Directory.GetCurrentDirectory()).Name;
      var assemblyDir =
        Path.Combine(Environment.CurrentDirectory, "bin", buildConfiguration, buildFramework, "publish");
      var assemblyPath = Path.Combine(assemblyDir, proj + ".dll");
      if (File.Exists(assemblyPath) == false)
        throw new ArgumentException($"Could not find an assembly at {assemblyPath}");

      _shellAsm = new ShellAssemblyLoadContext(assemblyDir);
      var assembly = _shellAsm.LoadFromAssemblyPath(assemblyPath);

      return MetaModelFactory.FromAssembly(assembly, name);
    }

    static void WriteError(object details)
    {
      var prev = Console.ForegroundColor;
      Console.ForegroundColor = ConsoleColor.Red;
      Console.WriteLine(details);
      Console.ForegroundColor = prev;
    }
    static void WriteVerbose(object details)
    {
      var prev = Console.ForegroundColor;
      Console.ForegroundColor = ConsoleColor.DarkGray;
      Console.WriteLine(details);
      Console.ForegroundColor = prev;
    }

    static void PrintLoadStep(LoadTestStepContext obj)
    {
      Console.WriteLine($"[{obj.ExecutionTime.Elapsed}] Beetles in flight at {obj.RequestsPerSecond}rps");
    }

    static string GetDefaultPackagePath(SquirtOptions options)
    {
      if (options.Rebuild)
        BuildPackage(options.Configuration, options.Framework, options.Verbose);
      var proj = new DirectoryInfo(Directory.GetCurrentDirectory()).Name;
      var filePath = Path.Combine(Environment.CurrentDirectory, "bin", options.Configuration, options.Framework, proj) +
                     ".zip";

      if (!File.Exists(filePath))
        throw new ArgumentException($"Could not find a package at {filePath}. Did you call 'dotnet lambda package'?");

      return filePath;
    }

    static void BuildPackage(string configuration, string framework, bool verbose = false)
    {
      var process = new Process
      {
        StartInfo = new ProcessStartInfo
        {
          FileName = "dotnet",
          Arguments = $"lambda package -c {configuration} -f {framework}"
        }
      };
      var color = Console.ForegroundColor;
      if (!verbose)
      {
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.RedirectStandardOutput = true;
      }
      else
      {
        Console.ForegroundColor = ConsoleColor.DarkGray;
      }

      process.Start();
      process.WaitForExit();

      Console.ForegroundColor = color;
      if (process.ExitCode != 0)
        throw new InvalidOperationException("Could not build the lambda");
    }
  }

  class ShellAssemblyLoadContext : AssemblyLoadContext
  {
    readonly Dictionary<AssemblyName, Assembly> _loadedAsm;

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
      return Default.LoadFromAssemblyName(assemblyName) ??
             (_loadedAsm.TryGetValue(assemblyName, out var asm) ? asm : null);
    }
  }
}