using System.Diagnostics;

namespace StarRatingRebirth;

internal class Program
{
    static void Main(string[] args)
    {
        string versionStr = $" (algorithm version: {SRCalculator.Version})";
        string creditStr = $"Star-Rating-Rebirth by [Crz]sunnyxxy{versionStr}";

        if (args.Contains("--version") || args.Contains("-V"))
        {
            Console.WriteLine(creditStr);
            return;
        }

        string mod = "";

        for (int i = 0; i < args.Length; i++)
        {
            if ((args[i] == "--mod" || args[i] == "-M") && i + 1 < args.Length)
            {
                mod = args[i + 1];
            }
        }

        string folderPath = Directory.GetCurrentDirectory();
        if (args.Length > 0 && !args[0].StartsWith("-"))
        {
            folderPath = Path.GetFullPath(args[0]);
        }

        if (!Directory.Exists(folderPath))
        {
            Console.WriteLine($"错误: {folderPath} 不是有效的目录。");
            return;
        }

        Console.WriteLine(creditStr);
        Console.WriteLine($"目录: {folderPath}, Mod: {mod}\n");

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        string[] osuFiles = Directory.GetFiles(folderPath, "*.osu");

        Parallel.ForEach(osuFiles, file =>
        {
            try
            {
                var data = ManiaData.FromFile(file);
                double sr = SRCalculator.Calculate(data, mod);
                Console.WriteLine($"({mod}) {Path.GetFileNameWithoutExtension(file)} | {sr:F4}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理文件 {Path.GetFileName(file)} 时出错: {ex.Message}");
            }
        });

        stopwatch.Stop();
        TimeSpan elapsed = stopwatch.Elapsed;

        Console.WriteLine($"\n计算完成，总耗时: {elapsed.TotalSeconds:F4}秒");
        Console.ReadLine();
    }
}
