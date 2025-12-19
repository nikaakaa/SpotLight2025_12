using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 基于 Simplex Noise 的地形生成器
/// 使用多层噪声叠加生成自然的大陆和海洋
/// 包含：fBm噪声 + 距离衰减 + Ridged Noise山脉 + 连通域清理
/// </summary>
public class NoiseTerrainGenerator : ITerrainGenerator
{
    // ========== 可调参数 ==========

    /// <summary>
    /// 噪声频率（越小大陆越完整，越大地形越碎）
    /// </summary>
    public float frequency = 0.04f;  // 适中频率生成多个大陆

    /// <summary>
    /// 噪声八度数量（越多细节越丰富）
    /// </summary>
    public int octaves = 8;

    /// <summary>
    /// 持续度（每层振幅衰减比例）
    /// </summary>
    public float persistence = 0.5f;

    /// <summary>
    /// 空隙度（每层频率增加比例）
    /// </summary>
    public float lacunarity = 2.0f;

    /// <summary>
    /// 大陆提升偏移（正值=更多陆地，负值=更多海洋）
    /// </summary>
    public float continentBias = -0.08f;  // 负值让海洋更多，大陆更分散

    // ========== 山脊参数 ==========

    /// <summary>
    /// 山脊噪声频率
    /// </summary>
    public float ridgeFrequency = 0.04f;

    /// <summary>
    /// 山脊强度（叠加到高度上）
    /// </summary>
    public float ridgeStrength = 0.12f;

    /// <summary>
    /// 山脊开始应用的高度阈值（只在高海拔陆地应用）
    /// </summary>
    public float ridgeMinHeight = 0.55f;

    // ========== 连通域清理参数 ==========

    /// <summary>
    /// 保留的最大陆地块数量
    /// </summary>
    public int maxContinents = 6;  // 保留多个大陆

    /// <summary>
    /// 小于此面积的陆地块会被淹没
    /// </summary>
    public int minLandmassSize = 30;  // 适中面积阈值

    // ========== 高度阈值 ==========

    public float deepWaterThreshold = 0.35f;
    public float shallowWaterThreshold = 0.45f;
    public float coastThreshold = 0.50f;  // 提高海岸阈值让海洋更多
    public float plainThreshold = 0.65f;
    public float hillThreshold = 0.78f;
    public float mountainThreshold = 0.90f;

    /// <summary>
    /// 生成地形
    /// </summary>
    public void Generate(HexGrid<TerrainCell> grid, int seed, int radius)
    {
        var noise = new SimplexNoise(seed);
        var ridgeNoise = new SimplexNoise(seed + 12345); // 山脊用不同的种子
        var continentNoise = new SimplexNoise(seed + 54321); // 大陆形状用不同的种子

        // 生成所有格子
        var allCoords = HexCoord.Zero.GetHexesInRange(radius);

        // 第一遍：生成基础高度 + 山脊
        foreach (var coord in allCoords)
        {
            float height = CalculateHeight(noise, ridgeNoise, continentNoise, coord, radius);
            TerrainType type = HeightToTerrainType(height);
            grid.SetCell(coord, new TerrainCell(type, height));
        }

        Debug.Log($"[NoiseTerrainGenerator] 基础生成完成: {allCoords.Count} 个格子");

        // 第二遍：连通域清理（保留大陆，淹没小岛）
        CleanupSmallLandmasses(grid, allCoords);

        // 统计
        var stats = new Dictionary<TerrainType, int>();
        foreach (var cell in grid.AllCells)
        {
            if (!stats.ContainsKey(cell.type)) stats[cell.type] = 0;
            stats[cell.type]++;
        }
        Debug.Log($"[NoiseTerrainGenerator] 地形统计: {string.Join(", ", stats)}");
    }

    /// <summary>
    /// 计算单个格子的高度
    /// </summary>
    private float CalculateHeight(SimplexNoise noise, SimplexNoise ridgeNoise, SimplexNoise continentNoise, HexCoord coord, int radius)
    {
        // 转换为世界坐标
        Vector3 worldPos = HexConverter2D.HexToWorld(coord);
        Vector2 noisePos = new Vector2(worldPos.x, worldPos.y);

        // 1. 大陆形状噪声（低频，决定大陆轮廓）
        float continentShape = continentNoise.Evaluate(noisePos * 0.015f);
        continentShape = (continentShape + 1f) / 2f; // 归一化到 0-1

        // 2. 细节噪声（中频，添加地形变化）
        float detailNoise = CalculateFBM(noise, noisePos);
        detailNoise = (detailNoise + 1f) / 2f; // 归一化到 0-1

        // 3. 混合大陆形状和细节（大陆形状权重高）
        float height = continentShape * 0.7f + detailNoise * 0.3f;

        // 4. 应用大陆偏移（让陆地更多）
        height += continentBias;

        // 5. 岛屿遮罩（强制边缘为海洋）
        float distanceFromCenter = coord.DistanceTo(HexCoord.Zero) / (float)radius;

        // 使用更平滑的衰减曲线
        if (distanceFromCenter > 0.5f)
        {
            // 从50%半径开始衰减，到100%完全是海洋
            float falloff = (distanceFromCenter - 0.5f) / 0.5f;
            falloff = falloff * falloff; // 二次方衰减更自然
            height -= falloff * 0.5f;
        }

        // 6. 边缘强制为深海
        if (distanceFromCenter > 0.85f)
        {
            float edgeFactor = (distanceFromCenter - 0.85f) / 0.15f;
            height -= edgeFactor * 0.3f;
        }

        // 7. 限制到 0-1
        height = Mathf.Clamp01(height);

        // 8. Ridged Noise 山脊（只在高海拔陆地应用）
        if (height > ridgeMinHeight)
        {
            float ridged = CalculateRidgedNoise(ridgeNoise, noisePos);
            // 越高的地方，山脊越强
            float ridgeFactor = (height - ridgeMinHeight) / (1f - ridgeMinHeight);
            height += ridged * ridgeStrength * ridgeFactor;
            height = Mathf.Clamp01(height);
        }

        return height;
    }


    /// <summary>
    /// 分形布朗运动（FBM）- 多层噪声叠加
    /// </summary>
    private float CalculateFBM(SimplexNoise noise, Vector2 pos)
    {
        float total = 0f;
        float amplitude = 1f;
        float freq = frequency;
        float maxValue = 0f;

        for (int i = 0; i < octaves; i++)
        {
            total += noise.Evaluate(pos * freq) * amplitude;
            maxValue += amplitude;

            amplitude *= persistence;
            freq *= lacunarity;
        }

        return total / maxValue;
    }

    /// <summary>
    /// Ridged Noise - 山脊噪声
    /// 对噪声取绝对值再反相，形成山脊线
    /// </summary>
    private float CalculateRidgedNoise(SimplexNoise noise, Vector2 pos)
    {
        float total = 0f;
        float amplitude = 1f;
        float freq = ridgeFrequency;
        float maxValue = 0f;

        for (int i = 0; i < 3; i++) // 3层山脊噪声
        {
            // 核心：取绝对值再反相
            float n = noise.Evaluate(pos * freq);
            n = 1f - Mathf.Abs(n);
            n = n * n; // 锐化山脊

            total += n * amplitude;
            maxValue += amplitude;

            amplitude *= 0.5f;
            freq *= 2f;
        }

        return total / maxValue;
    }

    /// <summary>
    /// 连通域清理 - 保留最大的几块陆地，淹没小岛
    /// </summary>
    private void CleanupSmallLandmasses(HexGrid<TerrainCell> grid, List<HexCoord> allCoords)
    {
        // 标记已访问
        var visited = new HashSet<HexCoord>();
        var landmasses = new List<List<HexCoord>>();

        // 找到所有陆地连通块
        foreach (var coord in allCoords)
        {
            if (visited.Contains(coord)) continue;

            var cell = grid.GetCell(coord);
            if (cell == null) continue;

            // 只处理陆地（非水域）
            if (cell.type == TerrainType.DeepWater || cell.type == TerrainType.ShallowWater)
            {
                visited.Add(coord);
                continue;
            }

            // Flood fill 找到整块陆地
            var landmass = FloodFillLand(grid, coord, visited);
            if (landmass.Count > 0)
            {
                landmasses.Add(landmass);
            }
        }

        // 按面积排序（从大到小）
        landmasses.Sort((a, b) => b.Count.CompareTo(a.Count));

        // 保留最大的 K 块，其他淹没
        for (int i = maxContinents; i < landmasses.Count; i++)
        {
            var landmass = landmasses[i];

            // 太小的陆地块淹没
            if (landmass.Count < minLandmassSize)
            {
                foreach (var coord in landmass)
                {
                    var cell = grid.GetCell(coord);
                    if (cell != null)
                    {
                        // 变成浅海
                        cell.type = TerrainType.ShallowWater;
                        cell.height = shallowWaterThreshold - 0.02f;
                    }
                }
            }
        }

        Debug.Log($"[NoiseTerrainGenerator] 连通域清理: 找到 {landmasses.Count} 块陆地，保留 {Mathf.Min(maxContinents, landmasses.Count)} 块");
    }

    /// <summary>
    /// Flood Fill 找到一块连通的陆地
    /// </summary>
    private List<HexCoord> FloodFillLand(HexGrid<TerrainCell> grid, HexCoord start, HashSet<HexCoord> visited)
    {
        var result = new List<HexCoord>();
        var queue = new Queue<HexCoord>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);

            // 检查所有邻居
            foreach (var neighbor in current.GetAllNeighbors())
            {
                if (visited.Contains(neighbor)) continue;

                var cell = grid.GetCell(neighbor);
                if (cell == null) continue;

                // 是陆地就加入
                if (cell.type != TerrainType.DeepWater && cell.type != TerrainType.ShallowWater)
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 高度值转换为地形类型
    /// </summary>
    private TerrainType HeightToTerrainType(float height)
    {
        if (height < deepWaterThreshold)
            return TerrainType.DeepWater;
        if (height < shallowWaterThreshold)
            return TerrainType.ShallowWater;
        if (height < coastThreshold)
            return TerrainType.Coast;
        if (height < plainThreshold)
            return TerrainType.Plain;
        if (height < hillThreshold)
            return TerrainType.Hill;
        if (height < mountainThreshold)
            return TerrainType.Mountain;

        return TerrainType.HighMountain;
    }
}

