using Newtonsoft.Json.Linq;

namespace StarRatingRebirth.Tests;

#pragma warning disable CS8618  // 不可为 null 的字段未初始化
#pragma warning disable CS8601  // 可能的 null 引用赋值
#pragma warning disable CS8602  // 解引用可能出现空引用
#pragma warning disable CS8604  // 可能的 null 引用参数
public class JsonData
{
    public double x { get; }
    public int K { get; }
    public int T { get; }
    public Note[] noteSeq { get; }
    public Note[][] noteSeqByCol { get; }
    public Note[] lnSeq { get; }
    public Note[] tailSeq { get; }
    public Note[][] lnSeqByCol { get; }

    public int[] baseCorners { get; }
    public int[] aCorners { get; }
    public int[] allCorners { get; }
    public bool[,] keyUsage { get; }
    public int[][] activeColumns { get; }
    public double[,] keyUsage400 { get; }

    public double[] anchor { get; }
    public double[,] deltaKs { get; }
    public double[] jBar { get; }
    public double[] xBar { get; }
    public int[] points { get; }
    public double[] cumsum { get; }
    public double[] values { get; }
    public double[] pBar { get; }
    public double[] aBar { get; }
    public double[] rBar { get; }

    public double[] cArr { get; }
    public double[] ksArr { get; }

    public JsonData(string filePath)
    {
        string jsonText = File.ReadAllText(filePath);
        var json = JObject.Parse(jsonText);

        x = json["x"].ToObject<double>();
        K = json["K"].ToObject<int>();
        T = json["T"].ToObject<int>();

        var noteSeqArray = json["note_seq"].ToObject<int[][]>();
        noteSeq = noteSeqArray.Select(arr => new Note(arr)).ToArray();

        var noteSeqByColArray = json["note_seq_by_column"].ToObject<int[][][]>();
        noteSeqByCol = noteSeqByColArray.Select(col =>
            col.Select(note => new Note(note)).ToArray()
        ).ToArray();

        var lnSeqArray = json["LN_seq"].ToObject<int[][]>();
        lnSeq = lnSeqArray.Select(arr => new Note(arr)).ToArray();

        var tailSeqArray = json["tail_seq"].ToObject<int[][]>();
        tailSeq = tailSeqArray.Select(arr => new Note(arr)).ToArray();

        var lnSeqByColArray = json["LN_seq_by_column"].ToObject<int[][][]>();
        lnSeqByCol = lnSeqByColArray.Select(col =>
            col.Select(note => new Note(note)).ToArray()
        ).ToArray();

        baseCorners = json["base_corners"].ToObject<int[]>();
        aCorners = json["A_corners"].ToObject<int[]>();
        allCorners = json["all_corners"].ToObject<int[]>();

        var keyUsageArray = json["key_usage"].ToObject<Dictionary<string, bool[]>>();
        keyUsage = new bool[K, baseCorners.Length];
        foreach (var kvp in keyUsageArray)
        {
            int key = int.Parse(kvp.Key);
            bool[] usage = kvp.Value;
            for (int t = 0; t < baseCorners.Length; t++)
            {
                keyUsage[key, t] = usage[t];
            }
        }

        activeColumns = json["active_columns"].ToObject<int[][]>();
        var keyUsage400Array = json["key_usage_400"].ToObject<Dictionary<string, double[]>>();
        keyUsage400 = new double[K, baseCorners.Length];
        foreach (var kvp in keyUsage400Array)
        {
            int key = int.Parse(kvp.Key);
            double[] usage = kvp.Value;
            for (int t = 0; t < baseCorners.Length; t++)
            {
                keyUsage400[key, t] = usage[t];
            }
        }

        anchor = json["anchor"].ToObject<double[]>();

        var deltaKsArray = json["delta_ks"].ToObject<Dictionary<string, double[]>>();
        deltaKs = new double[K, baseCorners.Length];
        foreach (var kvp in deltaKsArray)
        {
            int key = int.Parse(kvp.Key);
            double[] deltaK = kvp.Value;
            for (int t = 0; t < baseCorners.Length; t++)
            {
                deltaKs[key, t] = deltaK[t];
            }
        }

        jBar = json["Jbar"].ToObject<double[]>();
        xBar = json["Xbar"].ToObject<double[]>();

        var lnRepArray = json["LN_rep"].ToObject<JArray>();
        points = lnRepArray[0].ToObject<int[]>();
        cumsum = lnRepArray[1].ToObject<double[]>();
        values = lnRepArray[2].ToObject<double[]>();

        pBar = json["Pbar"].ToObject<double[]>();
        aBar = json["Abar"].ToObject<double[]>();
        rBar = json["Rbar"].ToObject<double[]>();

        cArr = json["C_arr"].ToObject<double[]>();
        ksArr = json["Ks_arr"].ToObject<double[]>();
    }
}
