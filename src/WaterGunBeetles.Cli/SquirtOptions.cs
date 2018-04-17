using System;
using CommandLine;

namespace WaterGunBeetles.Cli
{
  [Verb("squirt", HelpText = "Start a load test.")]
  class SquirtOptions
  {
    [Option('r', "rate", Required = true, HelpText = "Number of requests/s to achieve")]
    public int RequestsPerSecond { get; set; }

    [Option('d', "duration", Required = true, HelpText = "Length of time to run the load test for, for example '1m' or '1h'.")]
    public string Duration { get; set; }
    
    [Option('w', "warmup", Default = null, HelpText = "Duration of the warm-up.")]
    public string WarmUpTime { get; set; }

    [Option('n', "name", Required = false, HelpText = "Name of the Configuration object in the project. Defaults to the first one found.")]
    public string Name { get; set; }
    
    [Option('m', "memory", Default = 128, HelpText = "The memory size used for the lambda function")]
    public int MemorySize { get; set; }

    [Option('v', "verbose", Default = false, HelpText="Output verbose information")]
    public bool Verbose { get; set; }

    [Option("configuration", Default = "Release", HelpText="MSBuild configuration for builds and lambda packaging")]
    public string Configuration { get; set; }
    
    [Option("framework", Default="netcoreapp2.0", HelpText="MSBuild framework for builds and lambda packaging")]
    public string Framework { get; set; }
    
    [Option("rebuild", Default = false, HelpText="Rebuild the current project before runnign the load test.")]
    public bool Rebuild { get; set; }
    
    [Option(HelpText = "Path to a lambda package to upload")]
    public string PackagePath { get; set; }

    public void Validate()
    {
      if (WarmUpTime != null && Duration != null && Parser.Duration(WarmUpTime) > Parser.Duration(Duration))
        throw new ArgumentException($"{nameof(WarmUpTime)} cannot be less than the load test duration.");

    }
  }
}