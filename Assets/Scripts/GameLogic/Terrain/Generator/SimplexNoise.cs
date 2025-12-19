using UnityEngine;

/// <summary>
/// Simplex Noise 实现
/// 基于 Stefan Gustavson 的参考实现
/// 用于程序化生成自然地形
/// </summary>
public class SimplexNoise
{
    private readonly int[] perm = new int[512];
    private readonly int[] permMod12 = new int[512];

    // 梯度向量
    private static readonly int[][] Grad3 = new int[][]
    {
        new[] {1,1,0}, new[] {-1,1,0}, new[] {1,-1,0}, new[] {-1,-1,0},
        new[] {1,0,1}, new[] {-1,0,1}, new[] {1,0,-1}, new[] {-1,0,-1},
        new[] {0,1,1}, new[] {0,-1,1}, new[] {0,1,-1}, new[] {0,-1,-1}
    };

    // 偏斜因子
    private static readonly float F2 = 0.5f * (Mathf.Sqrt(3f) - 1f);
    private static readonly float G2 = (3f - Mathf.Sqrt(3f)) / 6f;

    public SimplexNoise(int seed)
    {
        var random = new System.Random(seed);
        var p = new int[256];

        for (int i = 0; i < 256; i++)
        {
            p[i] = i;
        }

        // Fisher-Yates 洗牌
        for (int i = 255; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (p[i], p[j]) = (p[j], p[i]);
        }

        for (int i = 0; i < 512; i++)
        {
            perm[i] = p[i & 255];
            permMod12[i] = perm[i] % 12;
        }
    }

    /// <summary>
    /// 计算 2D Simplex Noise
    /// </summary>
    /// <param name="x">X 坐标</param>
    /// <param name="y">Y 坐标</param>
    /// <returns>噪声值 (-1 到 1)</returns>
    public float Evaluate(float x, float y)
    {
        float n0, n1, n2;

        // 偏斜输入空间到简单网格
        float s = (x + y) * F2;
        int i = FastFloor(x + s);
        int j = FastFloor(y + s);

        float t = (i + j) * G2;
        float X0 = i - t;
        float Y0 = j - t;
        float x0 = x - X0;
        float y0 = y - Y0;

        int i1, j1;
        if (x0 > y0)
        {
            i1 = 1;
            j1 = 0;
        }
        else
        {
            i1 = 0;
            j1 = 1;
        }

        float x1 = x0 - i1 + G2;
        float y1 = y0 - j1 + G2;
        float x2 = x0 - 1f + 2f * G2;
        float y2 = y0 - 1f + 2f * G2;

        int ii = i & 255;
        int jj = j & 255;
        int gi0 = permMod12[ii + perm[jj]];
        int gi1 = permMod12[ii + i1 + perm[jj + j1]];
        int gi2 = permMod12[ii + 1 + perm[jj + 1]];

        float t0 = 0.5f - x0 * x0 - y0 * y0;
        if (t0 < 0)
        {
            n0 = 0f;
        }
        else
        {
            t0 *= t0;
            n0 = t0 * t0 * Dot(Grad3[gi0], x0, y0);
        }

        float t1 = 0.5f - x1 * x1 - y1 * y1;
        if (t1 < 0)
        {
            n1 = 0f;
        }
        else
        {
            t1 *= t1;
            n1 = t1 * t1 * Dot(Grad3[gi1], x1, y1);
        }

        float t2 = 0.5f - x2 * x2 - y2 * y2;
        if (t2 < 0)
        {
            n2 = 0f;
        }
        else
        {
            t2 *= t2;
            n2 = t2 * t2 * Dot(Grad3[gi2], x2, y2);
        }

        // 缩放到 [-1, 1]
        return 70f * (n0 + n1 + n2);
    }

    /// <summary>
    /// 计算 2D Simplex Noise (Vector2 重载)
    /// </summary>
    public float Evaluate(Vector2 pos)
    {
        return Evaluate(pos.x, pos.y);
    }

    private static int FastFloor(float x)
    {
        int xi = (int)x;
        return x < xi ? xi - 1 : xi;
    }

    private static float Dot(int[] g, float x, float y)
    {
        return g[0] * x + g[1] * y;
    }
}
