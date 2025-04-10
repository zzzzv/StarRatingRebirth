using System.Diagnostics;

namespace StarRatingRebirth;

internal class Program
{
    public enum Mod
    {
        NM, // No Mod
        DT, // Double Time
        HT  // Half Time
    }

    static void Main(string[] args)
    {
        // 显示版权信息和版本
        string versionStr = $" (algorithm version: {SRCalculator.Version})";
        string creditStr = $"Star-Rating-Rebirth by [Crz]sunnyxxy{versionStr}";

        // 解析命令行参数
        if (args.Contains("--version") || args.Contains("-V"))
        {
            Console.WriteLine(creditStr);
            return;
        }

        // 确定文件夹路径和Mod
        string folderPath = Directory.GetCurrentDirectory();
        Mod currentMod = Mod.NM;

        for (int i = 0; i < args.Length; i++)
        {
            if ((args[i] == "--mod" || args[i] == "-M") && i + 1 < args.Length)
            {
                if (Enum.TryParse(args[i + 1], true, out Mod parsedMod))
                {
                    currentMod = parsedMod;
                }
            }
        }

        // 如果提供了路径参数
        if (args.Length > 0 && !args[0].StartsWith("-"))
        {
            folderPath = args[0];
        }

        if (!Directory.Exists(folderPath))
        {
            Console.WriteLine($"错误: {folderPath} 不是有效的目录。");
            return;
        }

        Console.WriteLine(creditStr);
        Console.WriteLine($"目录: {folderPath}, Mod: {currentMod}\n");

        // 创建并启动计时器
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        // 执行星级计算
        CalculateStarRatings(folderPath, currentMod);

        // 停止计时器并显示耗时
        stopwatch.Stop();
        TimeSpan elapsed = stopwatch.Elapsed;

        Console.WriteLine($"\n计算完成，总耗时: {elapsed.TotalSeconds:F4}秒");
        Console.ReadLine();
    }

    static void CalculateStarRatings(string folderPath, Mod mod)
    {
        string[] osuFiles = Directory.GetFiles(folderPath, "*.osu");

        Parallel.ForEach(osuFiles, file =>
        {
            try
            {
                var data = ManiaData.FromFile(file);

                // 应用Mod
                switch (mod)
                {
                    case Mod.DT:
                        data = data.DT();
                        break;
                    case Mod.HT:
                        data = data.HT();
                        break;
                }

                double sr = SRCalculator.Calculate(data);
                Console.WriteLine($"({mod}) {Path.GetFileNameWithoutExtension(file)} | {sr:F4}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理文件 {Path.GetFileName(file)} 时出错: {ex.Message}");
            }
        });
    }
}
