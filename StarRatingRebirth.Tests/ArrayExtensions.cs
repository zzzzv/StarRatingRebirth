namespace StarRatingRebirth.Tests;

public static class ArrayExtensions
{
    // 比较二维数组的浮点数，带相对容差
    public static bool AreEqualWithTolerance(this double[,] array1, double[,] array2, double tolerance = 1e-6)
    {
        if (array1.GetLength(0) != array2.GetLength(0) || array1.GetLength(1) != array2.GetLength(1))
        {
            return false; // 尺寸不匹配
        }

        for (int i = 0; i < array1.GetLength(0); i++)
        {
            for (int j = 0; j < array1.GetLength(1); j++)
            {
                double a = array1[i, j];
                double b = array2[i, j];

                // 使用相对误差比较
                if (!AreAlmostEqual(a, b, tolerance))
                {
                    return false; // 超出容差范围
                }
            }
        }

        return true; // 所有元素都在容差范围内
    }

    // 比较一维数组的浮点数，带相对容差
    public static bool AreEqualWithTolerance(this double[] array1, double[] array2, double tolerance = 1e-6)
    {
        if (array1.Length != array2.Length)
        {
            return false; // 长度不匹配
        }

        for (int i = 0; i < array1.Length; i++)
        {
            double a = array1[i];
            double b = array2[i];

            // 使用相对误差比较
            if (!AreAlmostEqual(a, b, tolerance))
            {
                return false; // 超出容差范围
            }
        }

        return true; // 所有元素都在容差范围内
    }

    // 比较两个浮点数是否在容差范围内
    private static bool AreAlmostEqual(double a, double b, double tolerance)
    {
        if (a == b) return true; // 完全相等
        if (double.IsNaN(a) || double.IsNaN(b)) return false; // NaN 不相等
        if (double.IsInfinity(a) || double.IsInfinity(b)) return false; // 无穷大不相等

        // 计算相对误差
        double diff = Math.Abs(a - b);
        double largest = Math.Max(Math.Abs(a), Math.Abs(b));

        return diff <= tolerance * largest; // 相对误差小于容差
    }
}
