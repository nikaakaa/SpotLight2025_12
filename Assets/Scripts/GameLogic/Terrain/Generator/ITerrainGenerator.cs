/// <summary>
/// 地形生成器接口
/// </summary>
public interface ITerrainGenerator
{
    /// <summary>
    /// 生成地形
    /// </summary>
    /// <param name="grid">要填充的网格</param>
    /// <param name="seed">随机种子</param>
    /// <param name="radius">地图半径</param>
    void Generate(HexGrid<TerrainCell> grid, int seed, int radius);
}
