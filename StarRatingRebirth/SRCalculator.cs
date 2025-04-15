namespace StarRatingRebirth;

public class SRCalculator
{
    public static string Version = "2025/04/15";

    internal static void PreProcess(ManiaData data, out double x, out int K, out int T,
        out Note[] noteSeq, out Note[][] noteSeqByCol, out Note[] lnSeq, out Note[] tailSeq, out Note[][] lnSeqByCol)
    {
        K = data.CS;
        x = 0.3 * Math.Pow((64.5 - Math.Ceiling(data.OD * 3)) / 500, 0.5);
        x = Math.Min(x, 0.6 * (x - 0.09) + 0.09);

        // 排序一次，后续复用这个排序结果
        noteSeq = data.Notes.OrderBy(n => n.Head).ThenBy(n => n.Key).ToArray();

        // 创建包含 K 个空列表的数组
        List<Note>[] noteColumns = new List<Note>[K];
        List<Note>[] lnColumns = new List<Note>[K];
        List<Note> lnNotes = new List<Note>();

        // 初始化所有列表
        for (int k = 0; k < K; k++)
        {
            noteColumns[k] = new List<Note>();
            lnColumns[k] = new List<Note>();
        }

        // 单次遍历，同时填充 noteColumns 和 lnColumns
        foreach (var note in noteSeq)
        {
            int key = note.Key;
            if (key >= 0 && key < K) // 确保 key 在有效范围内
            {
                noteColumns[key].Add(note);

                if (note.IsLong)
                {
                    lnNotes.Add(note);
                    lnColumns[key].Add(note);
                }
            }
        }

        // 将列表转换为数组
        noteSeqByCol = new Note[K][];
        lnSeqByCol = new Note[K][];

        for (int k = 0; k < K; k++)
        {
            noteSeqByCol[k] = noteColumns[k].ToArray();
            lnSeqByCol[k] = lnColumns[k].ToArray();
        }

        // 获取长音符数组和按尾部时间排序的长音符数组
        lnSeq = lnNotes.ToArray();
        tailSeq = lnSeq.OrderBy(n => n.Tail).ToArray();

        // 计算总时长 T
        if (tailSeq.Length > 0)
        {
            T = Math.Max(noteSeq[^1].Head, tailSeq[^1].Tail) + 1;
        }
        else
        {
            T = noteSeq[^1].Head + 1;
        }
    }


    internal static void GetCorners(Note[] noteSeq, int T, out int[] baseCorners, out int[] aCorners, out int[] allCorners)
    {
        var baseSet = new HashSet<int>();
        foreach (var note in noteSeq)
        {
            baseSet.Add(note.Head);
            if (note.IsLong)
            {
                baseSet.Add(note.Tail);
            }
        }
        foreach (int s in baseSet.ToArray())
        {
            baseSet.Add(s + 501);
            baseSet.Add(s - 499);
            baseSet.Add(s + 1); // 在音符确切位置解决狄拉克增量问题
        }
        baseSet.Add(0);
        baseSet.Add(T);
        baseCorners = baseSet
            .Where(s => 0 <= s && s <= T)
            .OrderBy(s => s)
            .ToArray();

        var aSet = new HashSet<int>();
        foreach (var note in noteSeq)
        {
            aSet.Add(note.Head);
            if (note.IsLong)
            {
                aSet.Add(note.Tail);
            }
        }
        foreach (int s in aSet.ToArray())
        {
            aSet.Add(s + 1000);
            aSet.Add(s - 1000);
        }
        aSet.Add(0);
        aSet.Add(T);
        aCorners = aSet
            .Where(s => 0 <= s && s <= T)
            .OrderBy(s => s)
            .ToArray();

        allCorners = baseCorners.Union(aCorners)
            .OrderBy(s => s)
            .ToArray();
    }

    internal static bool[,] GetKeyUsage(Note[] noteSeq, int K, int T, int[] baseCorners)
    {
        bool[,] keyUsage = new bool[K, baseCorners.Length];
        foreach (var note in noteSeq)
        {
            int startTime = Math.Max(note.Head - 150, 0);
            int endTime = !note.IsLong ? note.Head + 150 : Math.Min(note.Tail + 150, T - 1);
            int leftIdx = Utils.SearchSortedLeft(baseCorners, startTime);
            int rightIdx = Utils.SearchSortedLeft(baseCorners, endTime);
            for (int i = leftIdx; i < rightIdx; i++)
            {
                keyUsage[note.Key, i] = true;
            }
        }
        return keyUsage;
    }

    internal static double[,] GetKeyUsage400(Note[] noteSeq, int K, int T, int[] baseCorners)
    {
        double[,] keyUsage400 = new double[K, baseCorners.Length];
        foreach (var note in noteSeq)
        {
            int startTime = Math.Max(note.Head, 0);
            int endTime = !note.IsLong ? note.Head : Math.Min(note.Tail, T - 1);
            int left400Idx = Utils.SearchSortedLeft(baseCorners, startTime - 400);
            int leftIdx = Utils.SearchSortedLeft(baseCorners, startTime);
            int rightIdx = Utils.SearchSortedLeft(baseCorners, endTime);
            int right400Idx = Utils.SearchSortedLeft(baseCorners, endTime + 400);
            for (int i = leftIdx; i < rightIdx; i++)
            {
                keyUsage400[note.Key, i] += 3.75 + Math.Min(endTime - startTime, 1500) / 150.0;
            }
            for (int i = left400Idx; i < leftIdx; i++)
            {
                double diff = baseCorners[i] - startTime;
                keyUsage400[note.Key, i] += 3.75 - 3.75 / (400.0 * 400.0) * diff * diff;
            }
            for (int i = rightIdx; i < right400Idx; i++)
            {
                double diff = baseCorners[i] - endTime;
                keyUsage400[note.Key, i] += 3.75 - 3.75 / (400.0 * 400.0) * diff * diff;
            }
        }
        return keyUsage400;
    }

    internal static double[] ComputeAnchor(int K, double[,] keyUsage400, int[] baseCorners)
    {
        double[] anchor = new double[baseCorners.Length];

        for (int idx = 0; idx < baseCorners.Length; idx++)
        {
            var nonzeroCounts = Enumerable.Range(0, K)
                .Select(k => keyUsage400[k, idx])
                .OrderByDescending(c => c)
                .Where(c => c != 0)
                .ToList();

            if (nonzeroCounts.Count > 1)
            {
                double walk = 0;
                double maxWalk = 0;

                for (int i = 0; i < nonzeroCounts.Count - 1; i++)
                {
                    double ratio = nonzeroCounts[i + 1] / nonzeroCounts[i];
                    walk += nonzeroCounts[i] * (1 - 4 * Math.Pow(0.5 - ratio, 2));
                    maxWalk += nonzeroCounts[i];
                }

                anchor[idx] = walk / maxWalk;
            }
            else
            {
                anchor[idx] = 0;
            }
        }

        for (int i = 0; i < anchor.Length; i++)
        {
            anchor[i] = 1 + Math.Min(anchor[i] - 0.18, 5 * Math.Pow(anchor[i] - 0.22, 3));
        }
        return anchor;
    }

    internal static void LNBodiesCountSparseRepresentation(Note[] lnSeq, int T,
        out int[] points, out double[] cumsum, out double[] values)
    {
        // 字典：索引 -> 长音符体变化量（转换前）
        Dictionary<int, double> diff = new Dictionary<int, double>();

        foreach (var ln in lnSeq)
        {
            int t0 = Math.Min(ln.Head + 60, ln.Tail);
            int t1 = Math.Min(ln.Head + 120, ln.Tail);

            diff[t0] = diff.GetValueOrDefault(t0, 0) + 1.3;
            diff[t1] = diff.GetValueOrDefault(t1, 0) + (-1.3 + 1); // t1处的净变化：从第一部分的-1.3，然后+1
            diff[ln.Tail] = diff.GetValueOrDefault(ln.Tail, 0) - 1;
        }

        // 断点是变化发生的时间点
        points = new[] { 0, T }.Concat(diff.Keys).Distinct().OrderBy(p => p).ToArray();

        // 构建分段常量值（转换后）和累积和
        values = new double[points.Length - 1];
        cumsum = new double[points.Length];
        cumsum[0] = 0; // 断点处的累积和
        double curr = 0.0;

        for (int i = 0; i < points.Length - 1; i++)
        {
            int t = points[i];
            // 如果在t处有变化，更新运行值
            if (diff.ContainsKey(t))
            {
                curr += diff[t];
            }

            double v = Math.Min(curr, 2.5 + 0.5 * curr);
            values[i] = v;
            // 计算区间[points[i], points[i+1])上的累积和
            int segLength = points[i + 1] - points[i];
            cumsum[i + 1] = cumsum[i] + segLength * v;
        }
    }

    internal static double LNSum(int a, int b, int[] points, double[] cumsum, double[] values)
    {
        // 定位包含 a 和 b 的分段
        int i = Utils.SearchSortedRight(points, a) - 1;
        int j = Utils.SearchSortedRight(points, b) - 1;

        double total = 0.0;
        if (i == j)
        {
            // a 和 b 在同一分段内
            total = (b - a) * values[i];
        }
        else
        {
            // 第一个分段：从 a 到第 i 个分段的末尾
            total += (points[i + 1] - a) * values[i];
            // i+1 到 j-1 之间的完整分段
            total += cumsum[j] - cumsum[i + 1];
            // 最后一个分段：从第 j 个分段的开始到 b
            total += (b - points[j]) * values[j];
        }

        return total;
    }


    internal static void ComputeJbar(int K, int T, double x, Note[][] noteSeqByColumn, int[] baseCorners,
        out double[,] deltaKs, out double[] jBar)
    {
        int cornersLength = baseCorners.Length;

        double[,] jKs = new double[K, cornersLength];
        deltaKs = new double[K, cornersLength];

        for (int k = 0; k < K; k++)
        {
            for (int i = 0; i < cornersLength; i++)
            {
                deltaKs[k, i] = 1e9;
            }
        }

        static double jackNerfer(double delta) => 1 - 7e-5 * Math.Pow(0.15 + Math.Abs(delta - 0.08), -4);

        for (int k = 0; k < K; k++)
        {
            var notes = noteSeqByColumn[k];
            for (int i = 0; i < notes.Length - 1; i++)
            {
                int start = notes[i].Head;
                int end = notes[i + 1].Head;

                int leftIdx = Utils.SearchSortedLeft(baseCorners, start);
                int rightIdx = Utils.SearchSortedLeft(baseCorners, end);

                if (leftIdx >= rightIdx) continue;

                double delta = 0.001 * (end - start);
                double val = (1 / delta) * (1 / (delta + 0.11 * Math.Pow(x, 1.0 / 4)));
                double jVal = val * jackNerfer(delta);

                for (int idx = leftIdx; idx < rightIdx; idx++)
                {
                    jKs[k, idx] = jVal;
                    deltaKs[k, idx] = delta;
                }
            }
        }
        // 对每列的J_ks进行平滑处理
        double[,] jBarKs = new double[K, cornersLength];
        for (int k = 0; k < K; k++)
        {
            double[] rowData = new double[cornersLength];
            for (int i = 0; i < cornersLength; i++)
            {
                rowData[i] = jKs[k, i];
            }

            double[] smoothed = Utils.SmoothOnCorners(baseCorners, rowData, 500, 0.001, "sum");

            for (int i = 0; i < cornersLength; i++)
            {
                jBarKs[k, i] = smoothed[i];
            }
        }
        // 使用加权平均聚合各列
        jBar = new double[cornersLength];
        for (int i = 0; i < cornersLength; i++)
        {
            double num = 0.0;
            double den = 0.0;

            for (int k = 0; k < K; k++)
            {
                double v = jBarKs[k, i];
                double w = 1 / deltaKs[k, i];

                num += Math.Pow(Math.Max(v, 0), 5) * w;
                den += w;
            }

            jBar[i] = num / Math.Max(1e-9, den);
            jBar[i] = Math.Pow(jBar[i], 1.0 / 5);
        }
    }

    internal static double[] ComputeXbar(int K, int T, double x, Note[][] noteSeqByColumn, int[][] activeColumns, int[] baseCorners)
    {
        // 创建交叉矩阵
        double[][] crossMatrix = [
            [-1],
            [0.075, 0.075],
            [0.125, 0.05, 0.125],
            [0.125, 0.125, 0.125, 0.125],
            [0.175, 0.25, 0.05, 0.25, 0.175],
            [0.175, 0.25, 0.175, 0.175, 0.25, 0.175],
            [0.225, 0.35, 0.25, 0.05, 0.25, 0.35, 0.225],
            [0.225, 0.35, 0.25, 0.225, 0.225, 0.25, 0.35, 0.225],
            [0.275, 0.45, 0.35, 0.25, 0.05, 0.25, 0.35, 0.45, 0.275],
            [0.275, 0.45, 0.35, 0.25, 0.275, 0.275, 0.25, 0.35, 0.45, 0.275],
            [0.325, 0.55, 0.45, 0.35, 0.25, 0.05, 0.25, 0.35, 0.45, 0.55, 0.325]
        ];

        int cornersLength = baseCorners.Length;

        // 初始化 X_ks 和 fast_cross
        double[,] xKs = new double[K + 1, cornersLength];
        double[,] fastCross = new double[K + 1, cornersLength];

        // 获取交叉系数
        double[] crossCoeff = crossMatrix[K];

        // 计算每列的 X_ks 和 fast_cross 值
        for (int k = 0; k <= K; k++)
        {
            // 根据不同情况选择要处理的音符
            Note[] notesInPair;
            if (k == 0)
            {
                notesInPair = noteSeqByColumn[0];
            }
            else if (k == K)
            {
                notesInPair = noteSeqByColumn[K - 1];
            }
            else
            {
                // 合并两列的音符并按时间排序
                var mergedNotes = new List<Note>();
                mergedNotes.AddRange(noteSeqByColumn[k - 1]);
                mergedNotes.AddRange(noteSeqByColumn[k]);
                notesInPair = mergedNotes.OrderBy(n => n.Head).ToArray();
            }

            // 处理相邻音符对
            for (int i = 1; i < notesInPair.Length; i++)
            {
                int start = notesInPair[i - 1].Head;
                int end = notesInPair[i].Head;

                int idxStart = Utils.SearchSortedLeft(baseCorners, start);
                int idxEnd = Utils.SearchSortedLeft(baseCorners, end);

                if (idxStart >= idxEnd) continue;

                double delta = 0.001 * (end - start);
                double val = 0.16 * Math.Pow(Math.Max(x, delta), -2);

                // 检查活跃列条件
                bool condition1 = !contains(activeColumns[idxStart], k - 1) && !contains(activeColumns[idxEnd], k - 1);
                bool condition2 = !contains(activeColumns[idxStart], k) && !contains(activeColumns[idxEnd], k);

                if (condition1 || condition2)
                {
                    val *= (1 - crossCoeff[k]);
                }

                // 设置值
                for (int idx = idxStart; idx < idxEnd; idx++)
                {
                    xKs[k, idx] = val;
                    fastCross[k, idx] = Math.Max(0, 0.4 * Math.Pow(Math.Max(delta, Math.Max(0.06, 0.75 * x)), -2) - 80);
                }
            }
        }

        // 计算 X_base
        double[] xBase = new double[cornersLength];
        for (int i = 0; i < cornersLength; i++)
        {
            // 第一部分：xKs 的加权和
            double sum1 = 0;
            for (int k = 0; k <= K; k++)
            {
                sum1 += xKs[k, i] * crossCoeff[k];
            }

            // 第二部分：fastCross 的平方根乘积和
            double sum2 = 0;
            for (int k = 0; k < K; k++)
            {
                sum2 += Math.Sqrt(fastCross[k, i] * crossCoeff[k] * fastCross[k + 1, i] * crossCoeff[k + 1]);
            }

            xBase[i] = sum1 + sum2;
        }

        // 应用平滑操作得到 Xbar
        double[] xBar = Utils.SmoothOnCorners(baseCorners, xBase, 500, 0.001, "sum");
        return xBar;
    }

    // 辅助方法：检查数组中是否包含指定值
    private static bool contains(int[] array, int value)
    {
        if (array == null) return false;
        foreach (int item in array)
        {
            if (item == value) return true;
        }
        return false;
    }

    internal static double[] ComputePbar(int K, int T, double x, Note[] noteSeq, int[] points, double[] cumsum, double[] values, double[] anchor, int[] baseCorners)
    {
        int cornersLength = baseCorners.Length;
        double[] pStep = new double[cornersLength];

        // stream_booster 函数的C#实现
        static double streamBooster(double delta)
        {
            double ratio = 7.5 / delta;
            return (160 < ratio && ratio < 360) ?
                1 + 1.7e-7 * (ratio - 160) * Math.Pow(ratio - 360, 2) :
                1;
        }

        for (int i = 0; i < noteSeq.Length - 1; i++)
        {
            int hL = noteSeq[i].Head;
            int hR = noteSeq[i + 1].Head;
            int deltaTime = hR - hL;

            if (deltaTime < 1e-9)
            {
                // 狄拉克增量情况：当音符同时出现时
                // 在基准网格中的音符头部精确位置添加尖峰
                double spike = 1000 * Math.Pow(0.02 * (4 / x - 24), 1.0 / 4);
                int leftIdx = Utils.SearchSortedLeft(baseCorners, hL);
                int rightIdx = Utils.SearchSortedRight(baseCorners, hL);

                for (int idx = leftIdx; idx < rightIdx; idx++)
                {
                    pStep[idx] += spike;
                }
            }
            else
            {
                // 对于delta_time > 0的常规情况，找出[h_l, h_r)范围内的基准网格索引
                int leftIdx = Utils.SearchSortedLeft(baseCorners, hL);
                int rightIdx = Utils.SearchSortedLeft(baseCorners, hR);

                if (leftIdx >= rightIdx) continue;

                double delta = 0.001 * deltaTime;
                double v = 1 + 6 * 0.001 * LNSum(hL, hR, points, cumsum, values);
                double bVal = streamBooster(delta);

                double inc;
                if (delta < 2 * x / 3)
                {
                    inc = (1 / delta) * Math.Pow(0.08 / x * (1 - 24 / x * Math.Pow(delta - x / 2, 2)), 1.0 / 4) * Math.Max(bVal, v);
                }
                else
                {
                    inc = (1 / delta) * Math.Pow(0.08 / x * (1 - 24 / x * Math.Pow(x / 6, 2)), 1.0 / 4) * Math.Max(bVal, v);
                }

                for (int idx = leftIdx; idx < rightIdx; idx++)
                {
                    pStep[idx] += Math.Min(inc * anchor[idx], Math.Max(inc, inc * 2 - 10));
                }
            }
        }

        double[] pBar = Utils.SmoothOnCorners(baseCorners, pStep, 500, 0.001, "sum");
        return pBar;
    }

    internal static double[] ComputeAbar(int K, int T, double x, Note[][] noteSeqByColumn, int[][] activeColumns, double[,] deltaKs, int[] aCorners, int[] baseCorners)
    {
        int cornersLength = baseCorners.Length;

        // 初始化dks数据结构，对应Python中的字典
        double[,] dks = new double[K - 1, cornersLength];

        // 填充dks数组
        for (int i = 0; i < cornersLength; i++)
        {
            int[] cols = activeColumns[i];
            for (int j = 0; j < cols.Length - 1; j++)
            {
                int k0 = cols[j];
                int k1 = cols[j + 1];

                // 使用之前在base_corners上计算的delta_ks
                dks[k0, i] = Math.Abs(deltaKs[k0, i] - deltaKs[k1, i]) + 0.4 * Math.Max(0, Math.Max(deltaKs[k0, i], deltaKs[k1, i]) - 0.11);
            }
        }

        // 初始化A_step数组，全部填充为1
        double[] aStep = new double[aCorners.Length];
        for (int i = 0; i < aCorners.Length; i++)
        {
            aStep[i] = 1.0;
        }

        // 修改A_step
        for (int i = 0; i < aCorners.Length; i++)
        {
            int s = aCorners[i];
            int idx = Utils.SearchSortedLeft(baseCorners, s);

            // 确保idx在有效范围内
            if (idx >= baseCorners.Length)
            {
                idx = baseCorners.Length - 1;
            }

            int[] cols = activeColumns[idx];
            for (int j = 0; j < cols.Length - 1; j++)
            {
                int k0 = cols[j];
                int k1 = cols[j + 1];
                double dVal = dks[k0, idx];

                if (dVal < 0.02)
                {
                    aStep[i] *= Math.Min(0.75 + 0.5 * Math.Max(deltaKs[k0, idx], deltaKs[k1, idx]), 1);
                }
                else if (dVal < 0.07)
                {
                    aStep[i] *= Math.Min(0.65 + 5 * dVal + 0.5 * Math.Max(deltaKs[k0, idx], deltaKs[k1, idx]), 1);
                }
                // 否则保持A_step[i]不变
            }
        }

        // 对A_step应用平滑操作得到Abar
        double[] aBar = Utils.SmoothOnCorners(aCorners, aStep, 250, mode: "avg");
        return aBar;
    }

    internal static double[] ComputeRbar(int K, int T, double x, Note[][] noteSeqByColumn, Note[] tailSeq, int[] baseCorners)
    {
        int cornersLength = baseCorners.Length;
        double[] iArr = new double[cornersLength];
        double[] rStep = new double[cornersLength];

        int[][] timesByColumn = new int[noteSeqByColumn.Length][];
        for (int i = 0; i < noteSeqByColumn.Length; i++)
        {
            timesByColumn[i] = noteSeqByColumn[i].Select(note => note.Head).ToArray();
        }

        // 计算释放指数(Release Index)
        List<double> iList = new List<double>();
        for (int i = 0; i < tailSeq.Length; i++)
        {
            int k = tailSeq[i].Key;
            int hI = tailSeq[i].Head;
            int tI = tailSeq[i].Tail;

            // 找到同一列中的下一个音符
            Note nextNote = Utils.FindNextNoteInColumn(tailSeq[i], timesByColumn[k], noteSeqByColumn);
            int hJ = nextNote.Head;

            double iH = 0.001 * Math.Abs(tI - hI - 80) / x;
            double iT = 0.001 * Math.Abs(hJ - tI - 80) / x;

            iList.Add(2 / (2 + Math.Exp(-5 * (iH - 0.75)) + Math.Exp(-5 * (iT - 0.75))));
        }

        // 对相邻尾音时间之间的每个区间，分配 I 和 R
        for (int i = 0; i < tailSeq.Length - 1; i++)
        {
            int tStart = tailSeq[i].Tail;
            int tEnd = tailSeq[i + 1].Tail;

            int leftIdx = Utils.SearchSortedLeft(baseCorners, tStart);
            int rightIdx = Utils.SearchSortedLeft(baseCorners, tEnd);

            if (leftIdx >= rightIdx) continue;

            // 设置这个区间内所有索引的值
            for (int idx = leftIdx; idx < rightIdx; idx++)
            {
                iArr[idx] = 1 + iList[i];
                double deltaR = 0.001 * (tailSeq[i + 1].Tail - tailSeq[i].Tail);
                rStep[idx] = 0.08 * Math.Pow(deltaR, -0.5) * (1 / x) * (1 + 0.8 * (iList[i] + iList[i + 1]));
            }
        }

        // 应用平滑操作得到Rbar
        double[] rBar = Utils.SmoothOnCorners(baseCorners, rStep, 500, 0.001, "sum");
        return rBar;
    }

    internal static void ComputeCAndKs(int K, int T, Note[] noteSeq, bool[,] keyUsage, int[] baseCorners,
    out double[] cStep, out double[] ksStep)
    {
        int cornersLength = baseCorners.Length;

        // 提取所有音符的命中时间并排序
        int[] noteHitTimes = noteSeq.Select(n => n.Head).OrderBy(t => t).ToArray();

        // 初始化 C_step 数组
        cStep = new double[cornersLength];

        // 计算每个基准点的 C(s)：500ms 内的音符数量
        for (int i = 0; i < cornersLength; i++)
        {
            int s = baseCorners[i];
            int low = s - 500;
            int high = s + 500;

            // 使用二分查找计算区间内的音符数量
            int highIdx = Utils.SearchSortedLeft(noteHitTimes, high);
            int lowIdx = Utils.SearchSortedLeft(noteHitTimes, low);

            cStep[i] = highIdx - lowIdx;
        }

        // 计算 Ks：本地按键使用计数（最小为1）
        ksStep = new double[cornersLength];
        for (int i = 0; i < cornersLength; i++)
        {
            int count = 0;
            for (int k = 0; k < K; k++)
            {
                if (keyUsage[k, i])
                {
                    count++;
                }
            }
            ksStep[i] = Math.Max(count, 1);
        }
    }

    public static double Calculate(ManiaData data)
    {
        // === 基本设置和解析 ===
        PreProcess(data, out double x, out int K, out int T,
                   out Note[] noteSeq, out Note[][] noteSeqByCol,
                   out Note[] lnSeq, out Note[] tailSeq, out Note[][] lnSeqByColumn);

        GetCorners(noteSeq, T, out int[] baseCorners, out int[] aCorners, out int[] allCorners);

        // 对于每一列，存储其使用情况（在150ms内是否非空）。例如：keyUsage[k, i]
        bool[,] keyUsage = GetKeyUsage(noteSeq, K, T, baseCorners);

        // 在base_corners的每个时间点，构建活跃列的列表
        int[][] activeColumns = Enumerable.Range(0, baseCorners.Length)
            .Select(i => Enumerable.Range(0, K)
                         .Where(k => keyUsage[k, i])
                         .ToArray())
            .ToArray();

        double[,] keyUsage400 = GetKeyUsage400(noteSeq, K, T, baseCorners);

        double[] anchor = ComputeAnchor(K, keyUsage400, baseCorners);

        ComputeJbar(K, T, x, noteSeqByCol, baseCorners, out double[,] deltaKs, out double[] jBar);
        double[] jBarInterp = Utils.InterpValues(allCorners, baseCorners, jBar);

        double[] xBar = ComputeXbar(K, T, x, noteSeqByCol, activeColumns, baseCorners);
        double[] xBarInterp = Utils.InterpValues(allCorners, baseCorners, xBar);

        // 构建LN主体的稀疏表示
        LNBodiesCountSparseRepresentation(lnSeq, T, out int[] points, out double[] cumsum, out double[] values);

        double[] pBar = ComputePbar(K, T, x, noteSeq, points, cumsum, values, anchor, baseCorners);
        double[] pBarInterp = Utils.InterpValues(allCorners, baseCorners, pBar);

        double[] aBar = ComputeAbar(K, T, x, noteSeqByCol, activeColumns, deltaKs, aCorners, baseCorners);
        double[] aBarInterp = Utils.InterpValues(allCorners, aCorners, aBar);

        double[] rBar = ComputeRbar(K, T, x, noteSeqByCol, tailSeq, baseCorners);
        double[] rBarInterp = Utils.InterpValues(allCorners, baseCorners, rBar);

        ComputeCAndKs(K, T, noteSeq, keyUsage, baseCorners, out double[] cStep, out double[] ksStep);
        double[] cArr = Utils.StepInterp(allCorners, baseCorners, cStep);
        double[] ksArr = Utils.StepInterp(allCorners, baseCorners, ksStep);

        // === 最终计算 ===
        // 在all_corners上计算难度D
        double[] sAll = new double[allCorners.Length];
        double[] tAll = new double[allCorners.Length];
        double[] dAll = new double[allCorners.Length];

        for (int i = 0; i < allCorners.Length; i++)
        {
            double term1 = Math.Pow(aBarInterp[i], 3 / ksArr[i]) * Math.Min(jBarInterp[i], 8 + 0.85 * jBarInterp[i]);
            double term2 = Math.Pow(aBarInterp[i], 2.0 / 3.0) * (0.8 * pBarInterp[i] + rBarInterp[i] * 35 / (cArr[i] + 8));

            sAll[i] = Math.Pow((0.4 * Math.Pow(term1, 1.5) + (1 - 0.4) * Math.Pow(term2, 1.5)), 2.0 / 3.0);
            tAll[i] = (Math.Pow(aBarInterp[i], 3 / ksArr[i]) * xBarInterp[i]) / (xBarInterp[i] + sAll[i] + 1);
            dAll[i] = 2.7 * Math.Pow(sAll[i], 0.5) * Math.Pow(tAll[i], 1.5) + sAll[i] * 0.27;
        }

        // 计算连续时间之间的间隔
        double[] gaps = new double[allCorners.Length];
        gaps[0] = (allCorners[1] - allCorners[0]) / 2.0;
        gaps[^1] = (allCorners[^1] - allCorners[^2]) / 2.0;
        for (int i = 1; i < allCorners.Length - 1; i++)
        {
            gaps[i] = (allCorners[i + 1] - allCorners[i - 1]) / 2.0;
        }

        // 每个拐角的有效权重是其密度和间隔的乘积
        double[] effectiveWeights = new double[allCorners.Length];
        for (int i = 0; i < allCorners.Length; i++)
        {
            effectiveWeights[i] = cArr[i] * gaps[i];
        }

        // 按难度D排序
        int[] sortedIndices = Enumerable.Range(0, allCorners.Length)
            .OrderBy(i => dAll[i])
            .ToArray();

        double[] dSorted = sortedIndices.Select(i => dAll[i]).ToArray();
        double[] wSorted = sortedIndices.Select(i => effectiveWeights[i]).ToArray();

        // 计算有效权重的累积和
        double[] cumWeights = new double[wSorted.Length];
        cumWeights[0] = wSorted[0];
        for (int i = 1; i < wSorted.Length; i++)
        {
            cumWeights[i] = cumWeights[i - 1] + wSorted[i];
        }
        double totalWeight = cumWeights[^1];

        double[] normCumWeights = cumWeights.Select(w => w / totalWeight).ToArray();

        // 计算目标百分位
        double[] targetPercentiles = { 0.945, 0.935, 0.925, 0.915, 0.845, 0.835, 0.825, 0.815 };
        int[] indices = new int[targetPercentiles.Length];

        for (int i = 0; i < targetPercentiles.Length; i++)
        {
            indices[i] = Array.FindIndex(normCumWeights, w => w >= targetPercentiles[i]);
            if (indices[i] < 0) indices[i] = normCumWeights.Length - 1;
        }

        double percentile93 = (dSorted[indices[0]] + dSorted[indices[1]] + dSorted[indices[2]] + dSorted[indices[3]]) / 4.0;
        double percentile83 = (dSorted[indices[4]] + dSorted[indices[5]] + dSorted[indices[6]] + dSorted[indices[7]]) / 4.0;

        // 计算加权平均值
        double weightedSum = 0;
        double weightSum = 0;
        for (int i = 0; i < dSorted.Length; i++)
        {
            weightedSum += Math.Pow(dSorted[i], 5) * wSorted[i];
            weightSum += wSorted[i];
        }
        double weightedMean = Math.Pow(weightedSum / weightSum, 1.0 / 5.0);

        // 最终SR计算
        double sr = (0.88 * percentile93) * 0.25 + (0.94 * percentile83) * 0.2 + weightedMean * 0.55;
        sr = Math.Pow(sr, 1.0) / Math.Pow(8, 1.0) * 8;

        double totalNotes = noteSeq.Length + 0.5 * lnSeq.Sum(ln => Math.Min(ln.Tail - ln.Head, 1000) / 200.0);
        sr *= totalNotes / (totalNotes + 60);

        sr = Utils.RescaleHigh(sr);
        sr *= 0.975;

        return sr;
    }

    public static double Calculate(ManiaData data, string mod)
    {
        switch (mod.ToUpper())
        {
            case "DT":
                data = data.DT();
                break;
            case "HT":
                data = data.HT();
                break;
        }
        return Calculate(data);
    }
}
