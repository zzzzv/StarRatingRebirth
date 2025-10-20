using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace StarRatingRebirth.Performance;

public class Program
{
    public static void Main(string[] args)
    {
        var config = ManualConfig.Create(DefaultConfig.Instance)
            .AddJob(Job.Default
                .WithLaunchCount(1)
                .WithWarmupCount(3)
                .WithIterationCount(5));
        var summary = BenchmarkRunner.Run<SRCalculatorBenchmarks>(config);
    }
}

[MemoryDiagnoser]
public class SRCalculatorBenchmarks
{
    private string[] _files = null!;
    private ManiaData[] _testData = null!;

    [GlobalSetup]
    public void Setup()
    {
        string path = "C:\\osu_tools\\TestSongs";
        _files = Directory.GetFiles(path, "*.osu", SearchOption.AllDirectories);

        _testData = _files.Select(f => ManiaData.FromFile(f)).ToArray();
    }

    [Benchmark]
    public double CalculateAllFiles()
    {
        double lastSR = -1;
        foreach (var file in _files)
        {
            var data = ManiaData.FromFile(file);
            lastSR = SRCalculator.Calculate(data);
        }
        return lastSR;
    }

    [Benchmark]
    public double CalculatePreloadedData()
    {
        double lastSR = -1;
        foreach (var data in _testData)
        {
            lastSR = SRCalculator.Calculate(data);
        }
        return lastSR;
    }

    [Benchmark]
    public double CalculateSingleFile()
    {
        var data = ManiaData.FromFile(_files[0]);
        return SRCalculator.Calculate(data);
    }
}