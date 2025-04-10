namespace StarRatingRebirth.Tests;

public static class ArrayExtensions
{
    // �Ƚ϶�ά����ĸ�������������ݲ�
    public static bool AreEqualWithTolerance(this double[,] array1, double[,] array2, double tolerance = 1e-6)
    {
        if (array1.GetLength(0) != array2.GetLength(0) || array1.GetLength(1) != array2.GetLength(1))
        {
            return false; // �ߴ粻ƥ��
        }

        for (int i = 0; i < array1.GetLength(0); i++)
        {
            for (int j = 0; j < array1.GetLength(1); j++)
            {
                double a = array1[i, j];
                double b = array2[i, j];

                // ʹ��������Ƚ�
                if (!AreAlmostEqual(a, b, tolerance))
                {
                    return false; // �����ݲΧ
                }
            }
        }

        return true; // ����Ԫ�ض����ݲΧ��
    }

    // �Ƚ�һά����ĸ�������������ݲ�
    public static bool AreEqualWithTolerance(this double[] array1, double[] array2, double tolerance = 1e-6)
    {
        if (array1.Length != array2.Length)
        {
            return false; // ���Ȳ�ƥ��
        }

        for (int i = 0; i < array1.Length; i++)
        {
            double a = array1[i];
            double b = array2[i];

            // ʹ��������Ƚ�
            if (!AreAlmostEqual(a, b, tolerance))
            {
                return false; // �����ݲΧ
            }
        }

        return true; // ����Ԫ�ض����ݲΧ��
    }

    // �Ƚ������������Ƿ����ݲΧ��
    private static bool AreAlmostEqual(double a, double b, double tolerance)
    {
        if (a == b) return true; // ��ȫ���
        if (double.IsNaN(a) || double.IsNaN(b)) return false; // NaN �����
        if (double.IsInfinity(a) || double.IsInfinity(b)) return false; // ��������

        // ����������
        double diff = Math.Abs(a - b);
        double largest = Math.Max(Math.Abs(a), Math.Abs(b));

        return diff <= tolerance * largest; // ������С���ݲ�
    }
}
