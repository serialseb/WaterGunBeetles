using CommandLine;

namespace WaterGunBeetles.Cli
{
  [Verb("squirt", HelpText = "Start a load test.")]
  class SquirtOptions
  {
    [Option]
    public string PackagePath { get; set; }

    [Option('m', "memory", Default = 128, HelpText = "The memory size used for the lambda function")]
    public int MemorySize { get; set; }

    [Option('r', "rps", Required = true, HelpText = "Number of requests/s to achieve")]
    public int RequestsPerSecond { get; set; }

    [Option('d', "duration", Required = true, HelpText = "Length of time to run the load test for, for example '1m' or '1h'.")]
    public string Duration { get; set; }

    [Option("rampto", Default = null, HelpText = "Number of request/s to ramp up to")]
    public int? RampUpTo { get; set; }

    [Option("verbose", Default = false)]
    public bool Verbose { get; set; }

    [Option("configuration", Default = "Release")]
    public string Configuration { get; set; }
    
    [Option("framework", Default="netcoreapp2.0")]
    public string Framework { get; set; }
    
    [Option("rebuild", Default = false)]
    public bool Rebuild { get; set; }
  }
}