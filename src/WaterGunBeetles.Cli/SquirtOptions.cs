using CommandLine;

namespace WaterGunBeetles.Cli
{
  [Verb("squirt", HelpText = "Start a load test.")]
  class SquirtOptions
  {
    [Option]
    public string PackagePath { get; set; }

    [Option('m', Default = 128, HelpText = "The memory size used for the lambda function")]
    public int MemorySize { get; set; }

    [Option('r', Required = true)]
    public int RequestsPerSecond { get; set; }

    [Option('d', Default = "1m")]
    public string Duration { get; set; }

    [Option('t', Default = null)]
    public int? RampUpTo { get; set; }
    
    [Option('v', Default=false)]
    public bool Verbose { get; set; }
  }
}