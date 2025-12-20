using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地形系统 - 管理六边形网格地形
/// </summary>
public class TerrainSystem
{
    public static TerrainSystem Instance { get; private set; }

    /// <summary>
    /// 地形网格
    /// </summary>
    public HexGrid<TerrainCell> terrainGrid = new HexGrid<TerrainCell>();

    /// <summary>
    /// 地图半径（六边形格子数）
    /// </summary>
    public int mapRadius;

    /// <summary>
    /// 随机种子
    /// </summary>
    public int seed;

    /// <summary>
    /// 地形生成器
    /// </summary>
    private ITerrainGenerator generator;

    public TerrainSystem()
    {
        Instance = this;
        generator = new NoiseTerrainGenerator();
    }

    public TerrainSystem(int seed, int radius) : this()
    {
        this.seed = seed;
        this.mapRadius = radius;
    }

    /// <summary>
    /// 设置地形生成器
    /// </summary>
    public void SetGenerator(ITerrainGenerator gen)
    {
        generator = gen;
    }

    /// <summary>
    /// 生成地形
    /// </summary>
    /// <param name="seed">随机种子（-1 表示随机）</param>
    /// <param name="radius">地图半径</param>
    public void GenerateTerrain(int seed = -1, int radius = 20)
    {
        if (seed < 0)
        {
            seed = Random.Range(0, int.MaxValue);
        }

        this.seed = seed;
        this.mapRadius = radius;

        terrainGrid.Clear();
        generator.Generate(terrainGrid, seed, radius);

        Debug.Log($"[TerrainSystem] 生成地形完成: seed={seed}, radius={radius}, cells={terrainGrid.Count}");
    }

    /// <summary>
    /// 获取指定坐标的地形
    /// </summary>
    public TerrainCell GetTerrainAt(HexCoord coord)
    {
        return terrainGrid.GetCell(coord);
    }

    /// <summary>
    /// 检查坐标是否可通过
    /// </summary>
    public bool IsPassable(HexCoord coord)
    {
        var cell = terrainGrid.GetCell(coord);
        return cell?.IsPassable ?? false;
    }

    /// <summary>
    /// 获取移动成本
    /// </summary>
    public float GetMoveCost(HexCoord coord)
    {
        var cell = terrainGrid.GetCell(coord);
        return cell?.MoveCost ?? float.MaxValue;
    }

    /// <summary>
    /// 获取建造成本倍率
    /// </summary>
    public float GetBuildCostMultiplier(HexCoord coord)
    {
        var cell = terrainGrid.GetCell(coord);
        return cell?.BuildCostMultiplier ?? 0f;
    }

    /// <summary>
    /// 检查是否可以放置城市
    /// </summary>
    public bool CanPlaceCity(HexCoord coord)
    {
        var cell = terrainGrid.GetCell(coord);
        return cell?.CanPlaceCity ?? false;
    }

    /// <summary>
    /// 获取所有可放置城市的坐标
    /// </summary>
    public List<HexCoord> GetAllCityPlaceableCoords()
    {
        var result = new List<HexCoord>();
        foreach (var coord in terrainGrid.AllCoords)
        {
            if (CanPlaceCity(coord))
            {
                result.Add(coord);
            }
        }
        return result;
    }

    /// <summary>
    /// 获取地形统计信息
    /// </summary>
    public Dictionary<TerrainType, int> GetTerrainStats()
    {
        var stats = new Dictionary<TerrainType, int>();
        foreach (var cell in terrainGrid.AllCells)
        {
            if (!stats.ContainsKey(cell.type))
            {
                stats[cell.type] = 0;
            }
            stats[cell.type]++;
        }
        return stats;
    }

    /// <summary>
    /// 清空地形
    /// </summary>
    public void Clear()
    {
        terrainGrid.Clear();
    }
}
