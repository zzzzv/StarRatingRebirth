namespace StarRatingRebirth;

internal static class VersionInfo
{
    public static string AlgorithmVersion => SRCalculator.Version;
    public static string ConsoleVersion => "0";
    public static string Version => $"{AlgorithmVersion}.{ConsoleVersion}";
}
