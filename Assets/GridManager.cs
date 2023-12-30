using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static Tile;
using static UnityEngine.EventSystems.EventTrigger;

public class GridManager : MonoBehaviour
{
    public int width = 8;
    public int height = 8;
    public List<Tile> tilePrefabs; // 存储所有类型方块的列表
    public GameObject cellPrefab;  // 格子预制体
    public static float tileSize = 0.8f;  // 方块间的间距
    float swapTime = 0.2f; //方块交换时间

    public SoundManager soundManager; //音效管理
    public AudioClip clearSound; // 消除音效剪辑
    public AudioClip lineSound; // 线音效剪辑
    public AudioClip burstSound; // 爆炸音效剪辑

    private Tile[,] tiles; //全部的方块

    public Dropdown dropdown; //下拉列表
    public int tileCount = 5; //方块类型数量
    public int score = 0; // 当前分数
    public Text scoreText; // UI Text 组件用于显示分数

    void Start()
    {
        CreateGrid();
        dropdown.onValueChanged.AddListener(delegate {
            DropdownValueChanged(dropdown);
        });
        AddScore(0);
    }

    void CreateGrid()
    {
        tiles = new Tile[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CreateCell(x, y); // 创建格子
                CreateTile(x, y); // 创建方块
            }
        }
    }//创建游戏物体
    void CreateCell(int x, int y)
    {
        // 实例化格子并设置为GridManager的子对象
        GameObject cell = Instantiate(cellPrefab, Vector3.zero, Quaternion.identity, this.transform);
        cell.transform.localPosition = new Vector3(x * tileSize, -y * tileSize, 0);
    } //创建格子当作方块背景
    void CreateTile(int x, int y)
    {
        Tile tilePrefab = GetRandomTilePrefab(x, y);
        Tile newTile = Instantiate(tilePrefab, Vector3.zero, Quaternion.identity, this.transform);
        newTile.transform.localPosition = new Vector3(x * tileSize, -y * tileSize, 0);
        newTile.Init(x, y, tilePrefab.tileType);
        tiles[x, y] = newTile;
    }

    Tile GetRandomTilePrefab(int x, int y)
    {
        Tile randomTile;
        bool hasAdjacentMatch;

        do
        {
            randomTile = tilePrefabs[Random.Range(0, tileCount)];
            hasAdjacentMatch = false;

            // 检查左边两个方块是否与随机选择的方块类型相同
            if (x >= 2 && tiles[x - 1, y].tileType == randomTile.tileType && tiles[x - 2, y].tileType == randomTile.tileType)
            {
                hasAdjacentMatch = true;
            }

            // 检查上方两个方块是否与随机选择的方块类型相同
            if (y >= 2 && tiles[x, y - 1].tileType == randomTile.tileType && tiles[x, y - 2].tileType == randomTile.tileType)
            {
                hasAdjacentMatch = true;
            }

        } while (hasAdjacentMatch);

        return randomTile;
    }
    private Tile selectedTile;
    private Tile targetTile;

    IEnumerator SwapTiles(Tile tile1, Tile tile2, bool swapBack = false)
    {
        // 在交换开始时，设置方块状态为Moving
        tile1.SetState(Tile.TileState.Moving);
        tile2.SetState(Tile.TileState.Moving);

        Vector3 startPos = tile1.transform.position;
        Vector3 endPos = tile2.transform.position;

        float elapsedTime = 0;

        while (elapsedTime < swapTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / swapTime;

            tile1.transform.position = Vector3.Lerp(startPos, endPos, t);
            tile2.transform.position = Vector3.Lerp(endPos, startPos, t);

            yield return null;
        }

        SwapTilePositions(tile1, tile2);

        // 在开始匹配检查前，更新状态为Checking
        tile1.SetState(Tile.TileState.Checking);
        tile2.SetState(Tile.TileState.Checking);

        if (!swapBack)
        {
            if (!CanTilesBeCleared(tile1, tile2))
            {
                StartCoroutine(SwapTiles(tile1, tile2, true)); // 交换回原位
            }
            else
            {
                // 更新状态为Idle
                tile1.SetState(Tile.TileState.Idle);
                tile2.SetState(Tile.TileState.Idle);
                ClearMatchedTiles(tile1, tile2);
            }
        }
        else
        {
            // 如果是交换回原位，直接更新状态为Idle
            tile1.SetState(Tile.TileState.Idle);
            tile2.SetState(Tile.TileState.Idle);
        }
    }
    void ClearMatchedTiles(Tile tile1, Tile tile2)
    {
        List<Tile> matchedTiles = GetMatchedTiles(tile1, tile2);

        StartCoroutine(ClearAndRefillBoardRoutine(matchedTiles));
    }
    IEnumerator ClearAndRefillBoardRoutine(List<Tile> matchedTiles)
    {
        soundManager.PlaySound(clearSound);
        
        // 直接播放所有匹配方块的消除动画
        foreach (var tile in matchedTiles)
        {
            if (tile != null)
            {
                if (tile.specialType != Tile.SpecialType.None)
                {
                    // 处理特殊方块的消除效果
                    ClearSpecialTile(tile);

                }
                // 普通方块的消除逻辑
                tile.SetState(Tile.TileState.Clearing);
                LockTilesAbove(tile);
            }
        }

        // 等待所有消除动画播放完毕
        bool allAnimationsCompleted = false;
        while (!allAnimationsCompleted)
        {
            allAnimationsCompleted = true;
            foreach (var tile in matchedTiles)
            {
                if (tile != null)
                {
                    Animator animator = tile.GetComponent<Animator>();
                    if (animator != null)
                    {
                        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                        // 确认已切换到消除动画并且动画播放完毕
                        if (!stateInfo.IsName("clear") || stateInfo.normalizedTime < 1.0f)
                        {
                            allAnimationsCompleted = false;
                            break;
                        }
                    }
                }
            }
            yield return null; // 等待下一帧
        }

        // 删除方块并在顶部生成新方块
        foreach (var tile in matchedTiles)
        {
            if (tile == null)
            {
                Debug.Log("错误删除：(" + tile.xIndex + "," + tile.yIndex + ")" + tile.currentState + " " + tile.tileType);
            }
            AddScore(1);
            Destroy(tile.gameObject);
        }
        yield return new WaitForSeconds(0.1f); // 等待一段时间确保方块被销毁
        // 生成新方块并使方块下落
        RefillBoard();
    }
    void ClearSpecialTile(Tile specialTile)
    {
        List<Tile> specialMatchedTiles = new List<Tile>();
        switch (specialTile.specialType)
        {
            case Tile.SpecialType.horizontal:
                for (int ix = 0; ix < width; ix++)
                {
                    Tile tile = tiles[ix, specialTile.yIndex];
                    if (specialTile != tile && tile.currentState != TileState.Clearing) //列表中不添加自身
                        specialMatchedTiles.Add(tile);
                }
                soundManager.PlaySound(lineSound);
                break;
            case Tile.SpecialType.vertical:
                for (int iy = 0; iy < height; iy++)
                {
                    Tile tile = tiles[specialTile.xIndex, iy];
                    if (specialTile != tile && tile.currentState != TileState.Clearing)
                        specialMatchedTiles.Add(tile);
                }
                soundManager.PlaySound(lineSound);
                break;
            case Tile.SpecialType.bomb:
                for (int ix = specialTile.xIndex - 1; ix <= specialTile.xIndex + 1; ix++)
                {
                    for (int iy = specialTile.yIndex - 1; iy <= specialTile.yIndex + 1; iy++)
                    {
                        // 检查边界
                        if (ix >= 0 && ix < width && iy >= 0 && iy < height)
                        {
                            // 销毁 3x3 区域内的方块
                            Tile tile = tiles[ix, iy];
                            if (specialTile != tile && tile.currentState != TileState.Clearing)
                                specialMatchedTiles.Add(tile);
                        }
                    }
                }
                soundManager.PlaySound(burstSound);
                break;
        }
        specialTile.PlaySpecialAnim();//消除前播放动画并把自己设为普通类型，防止无限递归
        StartCoroutine(ClearAndRefillBoardRoutine(specialMatchedTiles));
    }
    void RefillBoard()
    {
        StartCoroutine(RefillBoardRoutine());
    }

    IEnumerator RefillBoardRoutine()
    {
        for (int x = 0; x < width; x++)
        {
            int emptySpaceCount = 0;
            for (int y = height - 1; y >= 0; y--)
            {
                Tile currentTile = tiles[x, y];
                if (currentTile == null)
                {
                    emptySpaceCount++; // 累计空位数量
                }
                else if (emptySpaceCount > 0)
                {
                    // 如果上方有空位，则移动当前方块
                    tiles[x, y + emptySpaceCount] = currentTile;
                    tiles[x, y] = null;
                    currentTile.Init(x, y + emptySpaceCount, currentTile.tileType);
                    currentTile.StartFalling(emptySpaceCount);
                }
            }
            // 根据空位数量生成新方块
            for (int i = 1; i <= emptySpaceCount; i++)
            {
                CreateTileAtTop(x, -i, emptySpaceCount);
            }
        }

        // 等待所有方块下落完成
        yield return new WaitForSeconds(0.5f); // 根据下落动画时长调整
    }
    void CreateTileAtTop(int x, int startY, int fallDistance)
    {
        Tile tilePrefab = GetRandomTilePrefab();
        Tile newTile = Instantiate(tilePrefab, Vector3.zero, Quaternion.identity, this.transform);
        newTile.transform.localPosition = new Vector3(x * tileSize, -startY * tileSize, 0);
        newTile.Init(x, startY + fallDistance, tilePrefab.tileType); // 将新方块放置在目标位置
        tiles[x, startY + fallDistance] = newTile;
        newTile.StartFalling(fallDistance); // 新方块开始下落
    }
    void LockTilesAbove(Tile tile)
    {
        int tileYIndex = tile.yIndex;
        for (int y = 0; y < tileYIndex; y++)
        {
            Tile tileAbove = tiles[tile.xIndex, y];
            if (tileAbove != null)
            {
                tileAbove.SetState(Tile.TileState.Moving); // 锁定上方的方块
            }
        }
    }
    Tile GetRandomTilePrefab()
    {
        // 返回一个随机的方块预制体
        return tilePrefabs[Random.Range(0, tileCount)];
    }
    public void CheckForMatchesAt(int x, int y)
    {
        Tile currentTile = tiles[x, y];
        if (currentTile == null) return;

        List<Tile> matchedTiles = FindAllMatchedTiles(x, y, currentTile.tileType);
        if (matchedTiles.Count == 0)
        {
            return;
        }
        StartCoroutine(ClearAndRefillBoardRoutine(matchedTiles));
    }

    List<Tile> GetMatchedTiles(Tile tile1, Tile tile2)
    {
        // 收集所有匹配的方块
        List<Tile> matchedTiles = new List<Tile>();

        matchedTiles.AddRange(FindAllMatchedTiles(tile1.xIndex, tile1.yIndex, tile1.tileType));
        matchedTiles.AddRange(FindAllMatchedTiles(tile2.xIndex, tile2.yIndex, tile2.tileType));

        return matchedTiles.Distinct().ToList(); // 确保不重复计算相同的方块
    }

    List<Tile> FindAllMatchedTiles(int x, int y, Tile.TileType type)
    {
        List<Tile> matchedTiles = new List<Tile>();
        if (type == TileType.Null)
        {
            return matchedTiles;
        }

        // 检查水平和垂直方向的匹配
        var horizontalMatches = CheckMatchInDirection(x, y, type, Vector2Int.right)
            .Union(CheckMatchInDirection(x, y, type, Vector2Int.left)).ToList();
        var verticalMatches = CheckMatchInDirection(x, y, type, Vector2Int.up)
            .Union(CheckMatchInDirection(x, y, type, Vector2Int.down)).ToList();

        // 检测同时横竖排匹配条件（特殊方块3）
        if (horizontalMatches.Count >= 2 && verticalMatches.Count >= 2)
        {
            Tile tile = tiles[x, y];
            if (tile.specialType != Tile.SpecialType.None)
            {
                // 处理特殊方块的消除效果
                ClearSpecialTile(tile);
            }
            tile.SetAsSpecialTile(SpecialType.bomb); // 创建特殊方块3
            matchedTiles.AddRange(verticalMatches);
            matchedTiles.AddRange(horizontalMatches);
        }
        else if (horizontalMatches.Count >= 3) // 只有横向满足条件（特殊方块1）
        {
            Tile tile = tiles[x, y];
            if (tile.specialType != Tile.SpecialType.None)
            {
                // 处理特殊方块的消除效果
                ClearSpecialTile(tile);
            }
            tile.SetAsSpecialTile(SpecialType.vertical);
            matchedTiles.AddRange(horizontalMatches);
        }
        else if (verticalMatches.Count >= 3) // 只有纵向满足条件（特殊方块2）
        {
            Tile tile = tiles[x, y];
            if (tile.specialType != Tile.SpecialType.None)
            {
                // 处理特殊方块的消除效果
                ClearSpecialTile(tile);
            }
            tile.SetAsSpecialTile(SpecialType.horizontal);
            matchedTiles.AddRange(verticalMatches);
        }
        else if (horizontalMatches.Count >= 2)
        {
            matchedTiles.AddRange(horizontalMatches);
            matchedTiles.Add(tiles[x, y]); // 加入起始方块
        }
        else if (verticalMatches.Count >= 2)
        {
            matchedTiles.AddRange(verticalMatches);
            matchedTiles.Add(tiles[x, y]); // 加入起始方块
        }

        return matchedTiles.Distinct().ToList();
    }
    List<Tile> CheckMatchInDirection(int x, int y, Tile.TileType type, Vector2Int direction)
    {
        List<Tile> matchingTiles = new List<Tile>();

        int dx = x;
        int dy = y;

        while (true)
        {
            dx += direction.x;
            dy += direction.y;

            // 检查边界
            if (dx < 0 || dx >= width || dy < 0 || dy >= height)
                break;

            // 如果类型匹配，添加到列表
            if (tiles[dx, dy].tileType == type)
                matchingTiles.Add(tiles[dx, dy]);
            else
                break; // 如果类型不匹配，停止检查这个方向
        }

        return matchingTiles;
    }
    void SwapTilePositions(Tile tile1, Tile tile2)
    {
        // 交换网格中的位置
        int tempX = tile1.xIndex;
        int tempY = tile1.yIndex;
        tiles[tile1.xIndex, tile1.yIndex] = tile2;
        tiles[tile2.xIndex, tile2.yIndex] = tile1;

        // 更新方块的索引
        tile1.Init(tile2.xIndex, tile2.yIndex, tile1.tileType);
        tile2.Init(tempX, tempY, tile2.tileType);
    }

    bool CanTilesBeCleared(Tile tile1, Tile tile2)
    {
        return IsMatchAt(tile1.xIndex, tile1.yIndex, tile1.tileType) ||
               IsMatchAt(tile2.xIndex, tile2.yIndex, tile2.tileType);
    }

    bool IsMatchAt(int x, int y, Tile.TileType type)
    {
        // 检查水平和垂直方向是否有匹配
        return (CheckHorizontalMatch(x, y, type) || CheckVerticalMatch(x, y, type));
    }

    bool CheckHorizontalMatch(int x, int y, Tile.TileType type)
    {
        // 检查水平方向上是否有三个或更多相同类型的方块
        int matchCount = 1;

        // 向左检查
        for (int i = x - 1; i >= 0 && tiles[i, y].tileType == type; i--)
            matchCount++;

        // 向右检查
        for (int i = x + 1; i < width && tiles[i, y].tileType == type; i++)
            matchCount++;

        return matchCount >= 3;
    }

    bool CheckVerticalMatch(int x, int y, Tile.TileType type)
    {
        // 检查垂直方向上是否有三个或更多相同类型的方块
        int matchCount = 1;

        // 向下检查
        for (int i = y - 1; i >= 0 && tiles[x, i].tileType == type; i--)
            matchCount++;

        // 向上检查
        for (int i = y + 1; i < height && tiles[x, i].tileType == type; i++)
            matchCount++;

        return matchCount >= 3;
    }
    // 当玩家尝试交换方块时调用这个方法
    void TrySwapTiles(Tile tile1, Tile tile2)
    {
        if (tile1.currentState == Tile.TileState.Idle && tile2.currentState == Tile.TileState.Idle && AreAdjacent(tile1, tile2))
        {
            StartCoroutine(SwapTiles(tile1, tile2)); // 正常交换
        }
    }
    bool AreAdjacent(Tile tile1, Tile tile2)
    {
        // 检查两个方块是否在网格中相邻
        return (Mathf.Abs(tile1.xIndex - tile2.xIndex) == 1 && tile1.yIndex == tile2.yIndex) ||
               (Mathf.Abs(tile1.yIndex - tile2.yIndex) == 1 && tile1.xIndex == tile2.xIndex);
    }
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 startPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            selectedTile = GetTileAtPosition(startPos);
        }
        else if (Input.GetMouseButton(0) && selectedTile != null)
        {
            Vector2 currentPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            targetTile = GetTileAtPosition(currentPos);
            if (targetTile != null && selectedTile != targetTile && AreAdjacent(selectedTile, targetTile))
            {
                TrySwapTiles(selectedTile, targetTile);
                selectedTile = targetTile; // 更新选中的方块
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            selectedTile = null;
            targetTile = null;
        }
    }

    Tile GetTileAtPosition(Vector2 pos)
    {
        RaycastHit2D hit = Physics2D.Raycast(pos, Vector2.zero);
        if (hit.collider != null)
        {
            return hit.collider.transform.parent.GetComponent<Tile>();
        }
        return null;
    }

    public void AddScore(int points)
    {
        score += points;
        scoreText.text = score.ToString();
    }
    void DropdownValueChanged(Dropdown change)
    {
        int selectedValue = change.value + 1;
        // 根据 selectedValue 来更改方块类型数量
        Debug.Log("Selected Value: " + selectedValue);
        tileCount = selectedValue;
    }
}