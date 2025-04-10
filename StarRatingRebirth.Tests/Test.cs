namespace StarRatingRebirth.Tests;

public class Test
{
    [Theory]
    [InlineData("Data/1.osu", 7, 7.0, 6, 638, 1133)]
    public void FromFile(string filePath, int cs, double od, int key, int head, int tail)
    {
        var data = ManiaData.FromFile(filePath);
        Assert.Equal(cs, data.CS);
        Assert.Equal(od, data.OD);
        var n1 = data.Notes[1];
        Assert.Equal(key, n1.Key);
        Assert.Equal(head, n1.Head);
        Assert.Equal(tail, n1.Tail);
    }

    [Theory]
    [InlineData("Data/1.osu", 6.617692)]
    public void SR(string filePath, double expectedSR)
    {
        var data = ManiaData.FromFile(filePath);
        double sr = SRCalculator.Calculate(data);
        Assert.Equal(expectedSR, sr, 6);
    }

    [Theory]
    [InlineData("Data/1.osu", "Data/1.json")]
    public void TestCalculationInternals(string osuFilePath, string jsonFilePath)
    {
        var data = ManiaData.FromFile(osuFilePath);
        var json = new JsonData(jsonFilePath);

        SRCalculator.PreProcess(data, out double x, out int K, out int T, out Note[] noteSeq,
            out Note[][] noteSeqByCol, out Note[] lnSeq, out Note[] tailSeq, out Note[][] lnSeqByCol);
        Assert.Equal(json.x, x);
        Assert.Equal(json.K, K);
        Assert.Equal(json.T, T);
        Assert.Equal(json.noteSeq, noteSeq);
        Assert.Equal(json.noteSeqByCol, noteSeqByCol);
        Assert.Equal(json.lnSeq, lnSeq);
        Assert.Equal(json.tailSeq, tailSeq);
        Assert.Equal(json.lnSeqByCol, lnSeqByCol);

        SRCalculator.GetCorners(noteSeq, T, out int[] baseCorners, out int[] aCorners, out int[] allCorners);
        Assert.Equal(json.baseCorners, baseCorners);
        Assert.Equal(json.aCorners, aCorners);
        Assert.Equal(json.allCorners, allCorners);

        bool[,] keyUsage = SRCalculator.GetKeyUsage(noteSeq, K, T, baseCorners);
        Assert.Equal(json.keyUsage, keyUsage);

        int[][] activeColumns = Enumerable.Range(0, baseCorners.Length)
            .Select(i => Enumerable.Range(0, K)
                         .Where(k => keyUsage[k, i])
                         .ToArray())
            .ToArray();
        Assert.Equal(json.activeColumns, activeColumns);

        double[,] keyUsage400 = SRCalculator.GetKeyUsage400(noteSeq, K, T, baseCorners);
        Assert.True(keyUsage400.AreEqualWithTolerance(json.keyUsage400));

        double[] anchor = SRCalculator.ComputeAnchor(K, keyUsage400, baseCorners);
        Assert.True(anchor.AreEqualWithTolerance(json.anchor));

        SRCalculator.ComputeJbar(K, T, x, noteSeqByCol, baseCorners, out double[,] deltaKs, out double[] jBar);
        Assert.True(deltaKs.AreEqualWithTolerance(json.deltaKs));

        double[] jBarInterp = Utils.InterpValues(allCorners, baseCorners, jBar);
        Assert.True(jBarInterp.AreEqualWithTolerance(json.jBar));

        double[] xBar = SRCalculator.ComputeXbar(K, T, x, noteSeqByCol, activeColumns, baseCorners);
        double[] xBarInterp = Utils.InterpValues(allCorners, baseCorners, xBar);
        Assert.True(xBarInterp.AreEqualWithTolerance(json.xBar));

        SRCalculator.LNBodiesCountSparseRepresentation(lnSeq, T, out int[] points, out double[] cumsum, out double[] values);
        Assert.Equal(json.points, points);
        Assert.True(cumsum.AreEqualWithTolerance(json.cumsum));
        Assert.True(values.AreEqualWithTolerance(json.values));

        double[] pBar = SRCalculator.ComputePbar(K, T, x, noteSeq, points, cumsum, values, anchor, baseCorners);
        double[] pBarInterp = Utils.InterpValues(allCorners, baseCorners, pBar);
        Assert.True(pBarInterp.AreEqualWithTolerance(json.pBar));

        double[] aBar = SRCalculator.ComputeAbar(K, T, x, noteSeqByCol, activeColumns, deltaKs, aCorners, baseCorners);
        double[] aBarInterp = Utils.InterpValues(allCorners, aCorners, aBar);
        Assert.True(aBarInterp.AreEqualWithTolerance(json.aBar));

        double[] rBar = SRCalculator.ComputeRbar(K, T, x, noteSeqByCol, tailSeq, baseCorners);
        double[] rBarInterp = Utils.InterpValues(allCorners, baseCorners, rBar);
        Assert.True(rBarInterp.AreEqualWithTolerance(json.rBar));

        SRCalculator.ComputeCAndKs(K, T, noteSeq, keyUsage, baseCorners, out double[] cStep, out double[] ksStep);
        double[] cArr = Utils.StepInterp(allCorners, baseCorners, cStep);
        Assert.True(cArr.AreEqualWithTolerance(json.cArr));
        double[] ksArr = Utils.StepInterp(allCorners, baseCorners, ksStep);
        Assert.True(ksArr.AreEqualWithTolerance(json.ksArr));
    }
}
