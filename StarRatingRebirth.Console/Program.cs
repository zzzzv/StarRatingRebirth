using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using CsvHelper;
using System.Collections.Concurrent;
using System.Globalization;

namespace StarRatingRebirth;

internal class Program
{
    class Info
    {
        public required string File { get; set; }
        public int Key { get; set; }
        public double SR { get; set; }
        public double SR_HT { get; set; }
        public double SR_DT { get; set; }
        public string Error { get; set; } = string.Empty;
    }

    [STAThread]
    static void Main(string[] args)
    {
        Assembly? assembly = Assembly.GetAssembly(typeof(SRCalculator));
        string? description = assembly?.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description;
        if (description != null)
        {
            Console.WriteLine(description);
        }

        var path = Directory.GetCurrentDirectory();
        var files = GetOsuFiles(path);
        if (files.Length == 0)
        {
            Console.WriteLine("请选择包含 .osu 文件的根目录, 查找范围包括子文件夹");
            using var dialog = new FolderBrowserDialog
            {
                Description = "请选择包含 .osu 文件的根目录, 查找范围包括子文件夹",
                UseDescriptionForTitle = true,
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                path = dialog.SelectedPath;
                files = GetOsuFiles(path);
                if (files.Length == 0)
                {
                    Console.WriteLine($"目录 {path} 中没有找到 .osu 文件。");
                    return;
                }
            }
            else
            {
                Console.WriteLine("未选择文件夹，程序退出。");
                return;
            }
        }
        Console.WriteLine($"找到 {files.Length} 个 .osu 文件。");
        Console.Write("是否包括HT/DT计算？(y/N): ");
        bool includeHTDT = Console.ReadKey().Key == ConsoleKey.Y;
        Console.WriteLine();

        int success = 0;
        int error = 0;
        int notSupported = 0;
        int invalid = 0;
        var results = new ConcurrentBag<Info>();

        Console.WriteLine($"开始计算");
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        Parallel.ForEach(files, file =>
        {
            try
            {
                var data = ManiaData.FromFile(file);
                var info = new Info
                {
                    File = file,
                    Key = data.CS,
                    SR = SRCalculator.Calculate(data),
                };
                if (includeHTDT)
                {
                    info.SR_HT = SRCalculator.Calculate(data.HT());
                    info.SR_DT = SRCalculator.Calculate(data.DT());
                }
                results.Add(info);
                Interlocked.Increment(ref success);
            }
            catch (NotSupportedException)
            {
                Interlocked.Increment(ref notSupported);
            }
            catch (InvalidDataException)
            {
                Interlocked.Increment(ref invalid);
            }
            catch (Exception ex)
            {
                var info = new Info
                {
                    File = file,
                    Error = ex.Message
                };
                results.Add(info);
                Interlocked.Increment(ref error);
            }
            finally
            {
                int count = success + error + notSupported + invalid;
                if (count % 100 == 0)
                {
                    Console.Write($"\r已处理: {count}/{files.Length}");
                }
            }
        });

        stopwatch.Stop();
        TimeSpan elapsed = stopwatch.Elapsed;
        Console.WriteLine($"\n计算完成，总耗时: {elapsed.TotalSeconds:F4}秒");
        Console.WriteLine($"成功: {success}, 失败: {error}, 不支持: {notSupported}, 无效: {invalid}");

        var csvFile = $"{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        using (var writer = new StreamWriter(csvFile))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteHeader<Info>();
            csv.NextRecord();
            csv.WriteRecords(results);
        }
        Console.WriteLine($"结果已保存到 {csvFile}");
        Console.ReadLine();
    }

    private static string[] GetOsuFiles(string path)
    {
        if (!Directory.Exists(path))
        {
            Console.WriteLine($"目录 {path} 不存在。");
            return Array.Empty<string>();
        }
        string[] files = Directory.GetFiles(path, "*.osu", SearchOption.AllDirectories);
        if (files.Length == 0)
        {
            Console.WriteLine($"目录 {path} 中没有找到 .osu 文件。");
        }
        return files;
    }
}
