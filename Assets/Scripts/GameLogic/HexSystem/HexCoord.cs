using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 六边形坐标（使用 Cube 坐标系统）
/// Cube坐标满足约束: q + r + s = 0
/// </summary>
[Serializable]
public struct HexCoord : IEquatable<HexCoord>
{
    public int q; // 列坐标（东西方向）
    public int r; // 行坐标（东南-西北方向）
    public int s; // 第三轴（由 q + r + s = 0 约束）

    /// <summary>
    /// S 坐标（自动计算，满足 q + r + s = 0）
    /// </summary>
    public int S => -q - r;

    #region 构造函数

    /// <summary>
    /// 使用 Axial 坐标创建（自动计算 s）
    /// </summary>
    public HexCoord(int q, int r)
    {
        this.q = q;
        this.r = r;
        this.s = -q - r;
    }

    /// <summary>
    /// 使用 Cube 坐标创建
    /// </summary>
    public HexCoord(int q, int r, int s)
    {
        Debug.Assert(q + r + s == 0, "HexCoord: q + r + s 必须等于 0");
        this.q = q;
        this.r = r;
        this.s = s;
    }

    #endregion

    #region 静态常量

    /// <summary>
    /// 原点坐标
    /// </summary>
    public static readonly HexCoord Zero = new HexCoord(0, 0, 0);

    /// <summary>
    /// 六个方向的偏移量（Pointy-top 朝上的六边形）
    /// 顺序：东、东北、西北、西、西南、东南
    /// </summary>
    public static readonly HexCoord[] Directions = new HexCoord[]
    {
        new HexCoord(+1, 0, -1), // 东 (0)
        new HexCoord(+1, -1, 0), // 东北 (1)
        new HexCoord(0, -1, +1), // 西北 (2)
        new HexCoord(-1, 0, +1), // 西 (3)
        new HexCoord(-1, +1, 0), // 西南 (4)
        new HexCoord(0, +1, -1), // 东南 (5)
    };

    /// <summary>
    /// 对角线方向偏移量
    /// </summary>
    public static readonly HexCoord[] Diagonals = new HexCoord[]
    {
        new HexCoord(+2, -1, -1),
        new HexCoord(+1, -2, +1),
        new HexCoord(-1, -1, +2),
        new HexCoord(-2, +1, +1),
        new HexCoord(-1, +2, -1),
        new HexCoord(+1, +1, -2),
    };

    #endregion

    #region 运算符重载

    public static HexCoord operator +(HexCoord a, HexCoord b)
    {
        return new HexCoord(a.q + b.q, a.r + b.r, a.s + b.s);
    }

    public static HexCoord operator -(HexCoord a, HexCoord b)
    {
        return new HexCoord(a.q - b.q, a.r - b.r, a.s - b.s);
    }

    public static HexCoord operator *(HexCoord a, int k)
    {
        return new HexCoord(a.q * k, a.r * k, a.s * k);
    }

    public static bool operator ==(HexCoord a, HexCoord b)
    {
        return a.q == b.q && a.r == b.r;
    }

    public static bool operator !=(HexCoord a, HexCoord b)
    {
        return !(a == b);
    }

    #endregion

    #region 邻居与方向

    /// <summary>
    /// 获取指定方向的邻居
    /// </summary>
    /// <param name="direction">方向索引 (0-5)</param>
    public HexCoord GetNeighbor(int direction)
    {
        return this + Directions[direction];
    }

    /// <summary>
    /// 获取所有6个邻居
    /// </summary>
    public HexCoord[] GetAllNeighbors()
    {
        var neighbors = new HexCoord[6];
        for (int i = 0; i < 6; i++)
        {
            neighbors[i] = GetNeighbor(i);
        }
        return neighbors;
    }

    /// <summary>
    /// 获取指定对角线方向的格子
    /// </summary>
    public HexCoord GetDiagonalNeighbor(int direction)
    {
        return this + Diagonals[direction];
    }

    #endregion

    #region 距离计算

    /// <summary>
    /// 计算到另一个六边形的距离（格子数）
    /// </summary>
    public int DistanceTo(HexCoord other)
    {
        var diff = this - other;
        return (Mathf.Abs(diff.q) + Mathf.Abs(diff.r) + Mathf.Abs(diff.s)) / 2;
    }

    /// <summary>
    /// 计算到原点的距离
    /// </summary>
    public int Length()
    {
        return (Mathf.Abs(q) + Mathf.Abs(r) + Mathf.Abs(s)) / 2;
    }

    #endregion

    #region 范围查询

    /// <summary>
    /// 获取以当前坐标为中心，指定半径内的所有六边形
    /// </summary>
    public List<HexCoord> GetHexesInRange(int range)
    {
        var results = new List<HexCoord>();
        for (int dq = -range; dq <= range; dq++)
        {
            int r1 = Mathf.Max(-range, -dq - range);
            int r2 = Mathf.Min(range, -dq + range);
            for (int dr = r1; dr <= r2; dr++)
            {
                results.Add(new HexCoord(q + dq, r + dr));
            }
        }
        return results;
    }

    /// <summary>
    /// 获取距离正好为 radius 的环形六边形
    /// </summary>
    public List<HexCoord> GetHexRing(int radius)
    {
        var results = new List<HexCoord>();
        if (radius <= 0)
        {
            results.Add(this);
            return results;
        }

        // 从一个起始点开始
        var hex = this + Directions[4] * radius;

        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < radius; j++)
            {
                results.Add(hex);
                hex = hex.GetNeighbor(i);
            }
        }
        return results;
    }

    #endregion

    #region 路径与直线

    /// <summary>
    /// 获取从当前坐标到目标的直线路径（插值方式，可能多次拐弯）
    /// </summary>
    public List<HexCoord> LineTo(HexCoord target)
    {
        int distance = DistanceTo(target);
        var results = new List<HexCoord>();

        if (distance == 0)
        {
            results.Add(this);
            return results;
        }

        for (int i = 0; i <= distance; i++)
        {
            float t = (float)i / distance;
            results.Add(HexLerp(this, target, t));
        }
        return results;
    }

    /// <summary>
    /// 获取 L 形路径（只拐一次弯）
    /// 先沿一个方向走，再沿另一个方向走
    /// </summary>
    /// <param name="target">目标坐标</param>
    /// <param name="preferQ">true=先走Q轴变化，false=先走R轴变化</param>
    public List<HexCoord> LineToL(HexCoord target, bool preferQ = true)
    {
        var results = new List<HexCoord>();

        int dq = target.q - this.q;
        int dr = target.r - this.r;

        // 如果起点终点相同
        if (dq == 0 && dr == 0)
        {
            results.Add(this);
            return results;
        }

        HexCoord current = this;
        results.Add(current);

        // 找到拐点：L形路径的中间点
        HexCoord corner;
        if (preferQ)
        {
            // 先保持 r 不变，走到目标的 q
            corner = new HexCoord(target.q, this.r);
        }
        else
        {
            // 先保持 q 不变，走到目标的 r
            corner = new HexCoord(this.q, target.r);
        }

        // 从起点到拐点的直线
        if (current != corner)
        {
            var firstLeg = current.LineTo(corner);
            // 跳过第一个（已经添加了起点）
            for (int i = 1; i < firstLeg.Count; i++)
            {
                results.Add(firstLeg[i]);
            }
            current = corner;
        }

        // 从拐点到终点的直线
        if (current != target)
        {
            var secondLeg = current.LineTo(target);
            // 跳过第一个（已经添加了拐点）
            for (int i = 1; i < secondLeg.Count; i++)
            {
                results.Add(secondLeg[i]);
            }
        }

        return results;
    }

    /// <summary>
    /// 获取最优路径（结合转向代价 + 叉积偏好）
    /// - 转向代价：保持直行优先，转弯会增加代价
    /// - 叉积偏好：偏离起点-终点直线越远，代价越高
    /// </summary>
    public List<HexCoord> LineToLBest(HexCoord target)
    {
        var results = new List<HexCoord>();

        // 起点终点相同
        if (this == target)
        {
            results.Add(this);
            return results;
        }

        HexCoord current = this;
        results.Add(current);

        // 计算起点到终点的向量（用于叉积计算）
        Vector3 startPos = HexConverter2D.HexToWorld(this);
        Vector3 endPos = HexConverter2D.HexToWorld(target);
        Vector3 lineDir = endPos - startPos;
        float lineLength = lineDir.magnitude;

        // 上一步的移动方向（用于转向代价）
        int lastDirection = -1; // -1 表示还没有方向

        // 转向代价权重
        const float turnPenalty = 0.3f;
        // 叉积偏好权重
        const float crossPenalty = 0.001f;

        int maxSteps = this.DistanceTo(target) + 5;
        int steps = 0;

        while (current != target && steps < maxSteps)
        {
            steps++;
            Vector3 currentPos = HexConverter2D.HexToWorld(current);
            int currentDist = current.DistanceTo(target);

            // 找到最佳邻居
            HexCoord bestNeighbor = current;
            float bestScore = float.MaxValue;
            int bestDirection = -1;

            for (int i = 0; i < 6; i++)
            {
                HexCoord neighbor = current.GetNeighbor(i);
                int neighborDist = neighbor.DistanceTo(target);

                // 必须让距离减少（不走回头路）
                if (neighborDist >= currentDist)
                    continue;

                // 基础代价 = 距离
                float score = neighborDist;

                // === 转向代价 ===
                if (lastDirection >= 0 && i != lastDirection)
                {
                    // 计算转向角度（方向差）
                    int dirDiff = Mathf.Abs(i - lastDirection);
                    if (dirDiff > 3) dirDiff = 6 - dirDiff; // 取较小的角度差

                    // 转向越大，代价越高
                    score += dirDiff * turnPenalty;
                }

                // === 叉积偏好：偏离直线的程度 ===
                Vector3 neighborPos = HexConverter2D.HexToWorld(neighbor);
                Vector3 toNeighbor = neighborPos - startPos;

                // 2D叉积（在XY平面）：|A × B| = |Ax*By - Ay*Bx|
                float cross = Mathf.Abs(lineDir.x * toNeighbor.y - lineDir.y * toNeighbor.x);
                // 归一化（除以线段长度，得到点到直线的距离）
                float distToLine = cross / (lineLength + 0.001f);

                score += distToLine * crossPenalty;

                // 选择代价最低的
                if (score < bestScore)
                {
                    bestScore = score;
                    bestNeighbor = neighbor;
                    bestDirection = i;
                }
            }

            // 没找到更好的邻居
            if (bestNeighbor == current)
                break;

            current = bestNeighbor;
            lastDirection = bestDirection;
            results.Add(current);
        }

        return results;
    }

    #region A* 寻路（带转向代价）

    /// <summary>
    /// A* 节点，用于寻路
    /// </summary>
    private class AStarNode : IComparable<AStarNode>
    {
        public HexCoord Coord;
        public AStarNode Parent;
        public int Direction;   // 进入此节点的方向 (0-5)，-1表示起点
        public float G;         // 从起点到此节点的实际代价
        public float H;         // 启发式估计（到终点的代价）
        public float F => G + H;

        public int CompareTo(AStarNode other)
        {
            // 主排序：F 值（总代价）
            int fCompare = F.CompareTo(other.F);
            if (fCompare != 0) return fCompare;

            // 次级排序：H 值（优先选择离终点更近的）
            int hCompare = H.CompareTo(other.H);
            if (hCompare != 0) return hCompare;

            // 最终排序：坐标字典序（保证确定性）
            int qCompare = Coord.q.CompareTo(other.Coord.q);
            if (qCompare != 0) return qCompare;

            return Coord.r.CompareTo(other.Coord.r);
        }
    }

    /// <summary>
    /// A* 寻路算法（带转向代价 + 叉积偏好）
    /// 保证全局最优路径，尽量走直线，减少转弯
    /// </summary>
    /// <param name="target">目标坐标</param>
    /// <param name="turnPenalty">转向代价权重（默认0.3）</param>
    /// <param name="crossPenalty">偏离直线代价权重（默认0.001）</param>
    /// <returns>最优路径</returns>
    public List<HexCoord> FindPathAStar(HexCoord target, float turnPenalty = 0.3f, float crossPenalty = 0.001f)
    {
        return FindPathAStar(target, null, null, turnPenalty, crossPenalty);
    }

    /// <summary>
    /// A* 寻路算法（带障碍物避让 + 转向代价 + 叉积偏好）
    /// 保证全局最优路径，尽量走直线，减少转弯，同时避开已占用的格子
    /// </summary>
    /// <param name="target">目标坐标</param>
    /// <param name="blockedCoords">被阻挡的格子集合（其他航线已占用的格子）</param>
    /// <param name="allowedEndpoints">允许通过的端点集合（节点所在位置）</param>
    /// <param name="turnPenalty">转向代价权重（默认0.3）</param>
    /// <param name="crossPenalty">偏离直线代价权重（默认0.001）</param>
    /// <param name="blockedPenalty">经过被占用格子的额外代价（默认100，设为 float.MaxValue 则完全禁止）</param>
    /// <returns>最优路径</returns>
    public List<HexCoord> FindPathAStar(
        HexCoord target,
        HashSet<HexCoord> blockedCoords,
        HashSet<HexCoord> allowedEndpoints,
        float turnPenalty = 0.3f,
        float crossPenalty = 0.001f,
        float blockedPenalty = 100f)
    {
        var results = new List<HexCoord>();

        // 起点终点相同
        if (this == target)
        {
            results.Add(this);
            return results;
        }

        // 计算起点到终点的向量（用于叉积偏好）
        Vector3 startPos = HexConverter2D.HexToWorld(this);
        Vector3 endPos = HexConverter2D.HexToWorld(target);
        Vector3 lineDir = endPos - startPos;
        float lineLength = lineDir.magnitude;

        // Open 列表（待探索）和 Closed 集合（已探索）
        var openList = new List<AStarNode>();
        var closedSet = new HashSet<HexCoord>();
        // 用于快速查找节点（存储每个坐标的最佳节点）
        var nodeMap = new Dictionary<HexCoord, AStarNode>();

        // 起始节点
        var startNode = new AStarNode
        {
            Coord = this,
            Parent = null,
            Direction = -1,
            G = 0,
            H = CalculateHeuristic(this, target, startPos, lineDir, lineLength, crossPenalty)
        };
        openList.Add(startNode);
        nodeMap[this] = startNode;

        int maxIterations = 10000; // 防止无限循环
        int iterations = 0;

        while (openList.Count > 0 && iterations < maxIterations)
        {
            iterations++;

            // 找到 F 值最小的节点
            openList.Sort();
            var current = openList[0];
            openList.RemoveAt(0);

            // 到达目标
            if (current.Coord == target)
            {
                return ReconstructPath(current);
            }

            closedSet.Add(current.Coord);

            // 探索所有邻居
            for (int i = 0; i < 6; i++)
            {
                HexCoord neighborCoord = current.Coord.GetNeighbor(i);

                // 已经探索过
                if (closedSet.Contains(neighborCoord))
                    continue;

                // 检查是否被阻挡（但允许端点通过）
                bool isBlocked = blockedCoords != null && blockedCoords.Contains(neighborCoord);
                bool isAllowedEndpoint = allowedEndpoints != null && allowedEndpoints.Contains(neighborCoord);

                // 如果完全禁止通过被占用格子，且不是允许的端点，则跳过
                if (isBlocked && !isAllowedEndpoint && blockedPenalty >= float.MaxValue)
                    continue;

                // 计算 G 代价
                float moveCost = 1.0f; // 基础移动代价

                // 转向代价
                if (current.Direction >= 0 && i != current.Direction)
                {
                    int dirDiff = Mathf.Abs(i - current.Direction);
                    if (dirDiff > 3) dirDiff = 6 - dirDiff;
                    moveCost += dirDiff * turnPenalty;
                }

                // 被占用格子的额外代价（允许的端点不加代价）
                if (isBlocked && !isAllowedEndpoint)
                {
                    moveCost += blockedPenalty;
                }

                float tentativeG = current.G + moveCost;

                // 检查是否已在 Open 列表中
                AStarNode neighborNode;
                bool isNew = !nodeMap.TryGetValue(neighborCoord, out neighborNode);

                if (isNew || tentativeG < neighborNode.G)
                {
                    // 计算 H（启发式）
                    float h = CalculateHeuristic(neighborCoord, target, startPos, lineDir, lineLength, crossPenalty);

                    if (isNew)
                    {
                        neighborNode = new AStarNode
                        {
                            Coord = neighborCoord,
                            Parent = current,
                            Direction = i,
                            G = tentativeG,
                            H = h
                        };
                        nodeMap[neighborCoord] = neighborNode;
                        openList.Add(neighborNode);
                    }
                    else
                    {
                        // 更新现有节点
                        neighborNode.Parent = current;
                        neighborNode.Direction = i;
                        neighborNode.G = tentativeG;
                        neighborNode.H = h;
                    }
                }
            }
        }

        // 没找到路径，返回空
        return results;
    }

    /// <summary>
    /// 计算启发式函数（距离 + 叉积偏好）
    /// </summary>
    private float CalculateHeuristic(HexCoord coord, HexCoord target,
        Vector3 startPos, Vector3 lineDir, float lineLength, float crossPenalty)
    {
        // 基础启发式：六边形距离
        float h = coord.DistanceTo(target);

        // 叉积偏好：偏离起点-终点直线的程度
        Vector3 coordPos = HexConverter2D.HexToWorld(coord);
        Vector3 toCoord = coordPos - startPos;
        float cross = Mathf.Abs(lineDir.x * toCoord.y - lineDir.y * toCoord.x);
        float distToLine = cross / (lineLength + 0.001f);

        h += distToLine * crossPenalty;

        return h;
    }

    /// <summary>
    /// 从终点节点回溯重建路径
    /// </summary>
    private List<HexCoord> ReconstructPath(AStarNode endNode)
    {
        var path = new List<HexCoord>();
        var current = endNode;

        while (current != null)
        {
            path.Insert(0, current.Coord);
            current = current.Parent;
        }

        return path;
    }

    #endregion

    /// <summary>
    /// 六边形坐标插值
    /// </summary>
    private static HexCoord HexLerp(HexCoord a, HexCoord b, float t)
    {
        float q = Mathf.Lerp(a.q, b.q, t);
        float r = Mathf.Lerp(a.r, b.r, t);
        float s = Mathf.Lerp(a.s, b.s, t);
        return HexRound(q, r, s);
    }

    /// <summary>
    /// 浮点坐标四舍五入到最近的六边形
    /// </summary>
    public static HexCoord HexRound(float q, float r, float s)
    {
        int rq = Mathf.RoundToInt(q);
        int rr = Mathf.RoundToInt(r);
        int rs = Mathf.RoundToInt(s);

        float dq = Mathf.Abs(rq - q);
        float dr = Mathf.Abs(rr - r);
        float ds = Mathf.Abs(rs - s);

        // 调整误差最大的轴以满足 q + r + s = 0
        if (dq > dr && dq > ds)
        {
            rq = -rr - rs;
        }
        else if (dr > ds)
        {
            rr = -rq - rs;
        }
        else
        {
            rs = -rq - rr;
        }

        return new HexCoord(rq, rr, rs);
    }

    #endregion

    #region 旋转

    /// <summary>
    /// 绕原点顺时针旋转 60°
    /// </summary>
    public HexCoord RotateRight()
    {
        return new HexCoord(-r, -s, -q);
    }

    /// <summary>
    /// 绕原点逆时针旋转 60°
    /// </summary>
    public HexCoord RotateLeft()
    {
        return new HexCoord(-s, -q, -r);
    }

    #endregion

    #region 相等性

    public bool Equals(HexCoord other)
    {
        return q == other.q && r == other.r;
    }

    public override bool Equals(object obj)
    {
        return obj is HexCoord other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(q, r);
    }

    #endregion

    public override string ToString()
    {
        return $"Hex({q}, {r}, {s})";
    }
}
