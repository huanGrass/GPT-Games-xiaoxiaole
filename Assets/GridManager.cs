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
    public List<Tile> tilePrefabs; // �洢�������ͷ�����б�
    public GameObject cellPrefab;  // ����Ԥ����
    public static float tileSize = 0.8f;  // �����ļ��
    float swapTime = 0.2f; //���齻��ʱ��

    public SoundManager soundManager; //��Ч����
    public AudioClip clearSound; // ������Ч����
    public AudioClip lineSound; // ����Ч����
    public AudioClip burstSound; // ��ը��Ч����

    private Tile[,] tiles; //ȫ���ķ���

    public Dropdown dropdown; //�����б�
    public int tileCount = 5; //������������
    public int score = 0; // ��ǰ����
    public Text scoreText; // UI Text ���������ʾ����

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
                CreateCell(x, y); // ��������
                CreateTile(x, y); // ��������
            }
        }
    }//������Ϸ����
    void CreateCell(int x, int y)
    {
        // ʵ�������Ӳ�����ΪGridManager���Ӷ���
        GameObject cell = Instantiate(cellPrefab, Vector3.zero, Quaternion.identity, this.transform);
        cell.transform.localPosition = new Vector3(x * tileSize, -y * tileSize, 0);
    } //�������ӵ������鱳��
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

            // ���������������Ƿ������ѡ��ķ���������ͬ
            if (x >= 2 && tiles[x - 1, y].tileType == randomTile.tileType && tiles[x - 2, y].tileType == randomTile.tileType)
            {
                hasAdjacentMatch = true;
            }

            // ����Ϸ����������Ƿ������ѡ��ķ���������ͬ
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
        // �ڽ�����ʼʱ�����÷���״̬ΪMoving
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

        // �ڿ�ʼƥ����ǰ������״̬ΪChecking
        tile1.SetState(Tile.TileState.Checking);
        tile2.SetState(Tile.TileState.Checking);

        if (!swapBack)
        {
            if (!CanTilesBeCleared(tile1, tile2))
            {
                StartCoroutine(SwapTiles(tile1, tile2, true)); // ������ԭλ
            }
            else
            {
                // ����״̬ΪIdle
                tile1.SetState(Tile.TileState.Idle);
                tile2.SetState(Tile.TileState.Idle);
                ClearMatchedTiles(tile1, tile2);
            }
        }
        else
        {
            // ����ǽ�����ԭλ��ֱ�Ӹ���״̬ΪIdle
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
        
        // ֱ�Ӳ�������ƥ�䷽�����������
        foreach (var tile in matchedTiles)
        {
            if (tile != null)
            {
                if (tile.specialType != Tile.SpecialType.None)
                {
                    // �������ⷽ�������Ч��
                    ClearSpecialTile(tile);

                }
                // ��ͨ����������߼�
                tile.SetState(Tile.TileState.Clearing);
                LockTilesAbove(tile);
            }
        }

        // �ȴ��������������������
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
                        // ȷ�����л��������������Ҷ����������
                        if (!stateInfo.IsName("clear") || stateInfo.normalizedTime < 1.0f)
                        {
                            allAnimationsCompleted = false;
                            break;
                        }
                    }
                }
            }
            yield return null; // �ȴ���һ֡
        }

        // ɾ�����鲢�ڶ��������·���
        foreach (var tile in matchedTiles)
        {
            if (tile == null)
            {
                Debug.Log("����ɾ����(" + tile.xIndex + "," + tile.yIndex + ")" + tile.currentState + " " + tile.tileType);
            }
            AddScore(1);
            Destroy(tile.gameObject);
        }
        yield return new WaitForSeconds(0.1f); // �ȴ�һ��ʱ��ȷ�����鱻����
        // �����·��鲢ʹ��������
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
                    if (specialTile != tile && tile.currentState != TileState.Clearing) //�б��в��������
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
                        // ���߽�
                        if (ix >= 0 && ix < width && iy >= 0 && iy < height)
                        {
                            // ���� 3x3 �����ڵķ���
                            Tile tile = tiles[ix, iy];
                            if (specialTile != tile && tile.currentState != TileState.Clearing)
                                specialMatchedTiles.Add(tile);
                        }
                    }
                }
                soundManager.PlaySound(burstSound);
                break;
        }
        specialTile.PlaySpecialAnim();//����ǰ���Ŷ��������Լ���Ϊ��ͨ���ͣ���ֹ���޵ݹ�
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
                    emptySpaceCount++; // �ۼƿ�λ����
                }
                else if (emptySpaceCount > 0)
                {
                    // ����Ϸ��п�λ�����ƶ���ǰ����
                    tiles[x, y + emptySpaceCount] = currentTile;
                    tiles[x, y] = null;
                    currentTile.Init(x, y + emptySpaceCount, currentTile.tileType);
                    currentTile.StartFalling(emptySpaceCount);
                }
            }
            // ���ݿ�λ���������·���
            for (int i = 1; i <= emptySpaceCount; i++)
            {
                CreateTileAtTop(x, -i, emptySpaceCount);
            }
        }

        // �ȴ����з����������
        yield return new WaitForSeconds(0.5f); // �������䶯��ʱ������
    }
    void CreateTileAtTop(int x, int startY, int fallDistance)
    {
        Tile tilePrefab = GetRandomTilePrefab();
        Tile newTile = Instantiate(tilePrefab, Vector3.zero, Quaternion.identity, this.transform);
        newTile.transform.localPosition = new Vector3(x * tileSize, -startY * tileSize, 0);
        newTile.Init(x, startY + fallDistance, tilePrefab.tileType); // ���·��������Ŀ��λ��
        tiles[x, startY + fallDistance] = newTile;
        newTile.StartFalling(fallDistance); // �·��鿪ʼ����
    }
    void LockTilesAbove(Tile tile)
    {
        int tileYIndex = tile.yIndex;
        for (int y = 0; y < tileYIndex; y++)
        {
            Tile tileAbove = tiles[tile.xIndex, y];
            if (tileAbove != null)
            {
                tileAbove.SetState(Tile.TileState.Moving); // �����Ϸ��ķ���
            }
        }
    }
    Tile GetRandomTilePrefab()
    {
        // ����һ������ķ���Ԥ����
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
        // �ռ�����ƥ��ķ���
        List<Tile> matchedTiles = new List<Tile>();

        matchedTiles.AddRange(FindAllMatchedTiles(tile1.xIndex, tile1.yIndex, tile1.tileType));
        matchedTiles.AddRange(FindAllMatchedTiles(tile2.xIndex, tile2.yIndex, tile2.tileType));

        return matchedTiles.Distinct().ToList(); // ȷ�����ظ�������ͬ�ķ���
    }

    List<Tile> FindAllMatchedTiles(int x, int y, Tile.TileType type)
    {
        List<Tile> matchedTiles = new List<Tile>();
        if (type == TileType.Null)
        {
            return matchedTiles;
        }

        // ���ˮƽ�ʹ�ֱ�����ƥ��
        var horizontalMatches = CheckMatchInDirection(x, y, type, Vector2Int.right)
            .Union(CheckMatchInDirection(x, y, type, Vector2Int.left)).ToList();
        var verticalMatches = CheckMatchInDirection(x, y, type, Vector2Int.up)
            .Union(CheckMatchInDirection(x, y, type, Vector2Int.down)).ToList();

        // ���ͬʱ������ƥ�����������ⷽ��3��
        if (horizontalMatches.Count >= 2 && verticalMatches.Count >= 2)
        {
            Tile tile = tiles[x, y];
            if (tile.specialType != Tile.SpecialType.None)
            {
                // �������ⷽ�������Ч��
                ClearSpecialTile(tile);
            }
            tile.SetAsSpecialTile(SpecialType.bomb); // �������ⷽ��3
            matchedTiles.AddRange(verticalMatches);
            matchedTiles.AddRange(horizontalMatches);
        }
        else if (horizontalMatches.Count >= 3) // ֻ�к����������������ⷽ��1��
        {
            Tile tile = tiles[x, y];
            if (tile.specialType != Tile.SpecialType.None)
            {
                // �������ⷽ�������Ч��
                ClearSpecialTile(tile);
            }
            tile.SetAsSpecialTile(SpecialType.vertical);
            matchedTiles.AddRange(horizontalMatches);
        }
        else if (verticalMatches.Count >= 3) // ֻ�������������������ⷽ��2��
        {
            Tile tile = tiles[x, y];
            if (tile.specialType != Tile.SpecialType.None)
            {
                // �������ⷽ�������Ч��
                ClearSpecialTile(tile);
            }
            tile.SetAsSpecialTile(SpecialType.horizontal);
            matchedTiles.AddRange(verticalMatches);
        }
        else if (horizontalMatches.Count >= 2)
        {
            matchedTiles.AddRange(horizontalMatches);
            matchedTiles.Add(tiles[x, y]); // ������ʼ����
        }
        else if (verticalMatches.Count >= 2)
        {
            matchedTiles.AddRange(verticalMatches);
            matchedTiles.Add(tiles[x, y]); // ������ʼ����
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

            // ���߽�
            if (dx < 0 || dx >= width || dy < 0 || dy >= height)
                break;

            // �������ƥ�䣬��ӵ��б�
            if (tiles[dx, dy].tileType == type)
                matchingTiles.Add(tiles[dx, dy]);
            else
                break; // ������Ͳ�ƥ�䣬ֹͣ����������
        }

        return matchingTiles;
    }
    void SwapTilePositions(Tile tile1, Tile tile2)
    {
        // ���������е�λ��
        int tempX = tile1.xIndex;
        int tempY = tile1.yIndex;
        tiles[tile1.xIndex, tile1.yIndex] = tile2;
        tiles[tile2.xIndex, tile2.yIndex] = tile1;

        // ���·��������
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
        // ���ˮƽ�ʹ�ֱ�����Ƿ���ƥ��
        return (CheckHorizontalMatch(x, y, type) || CheckVerticalMatch(x, y, type));
    }

    bool CheckHorizontalMatch(int x, int y, Tile.TileType type)
    {
        // ���ˮƽ�������Ƿ��������������ͬ���͵ķ���
        int matchCount = 1;

        // ������
        for (int i = x - 1; i >= 0 && tiles[i, y].tileType == type; i--)
            matchCount++;

        // ���Ҽ��
        for (int i = x + 1; i < width && tiles[i, y].tileType == type; i++)
            matchCount++;

        return matchCount >= 3;
    }

    bool CheckVerticalMatch(int x, int y, Tile.TileType type)
    {
        // ��鴹ֱ�������Ƿ��������������ͬ���͵ķ���
        int matchCount = 1;

        // ���¼��
        for (int i = y - 1; i >= 0 && tiles[x, i].tileType == type; i--)
            matchCount++;

        // ���ϼ��
        for (int i = y + 1; i < height && tiles[x, i].tileType == type; i++)
            matchCount++;

        return matchCount >= 3;
    }
    // ����ҳ��Խ�������ʱ�����������
    void TrySwapTiles(Tile tile1, Tile tile2)
    {
        if (tile1.currentState == Tile.TileState.Idle && tile2.currentState == Tile.TileState.Idle && AreAdjacent(tile1, tile2))
        {
            StartCoroutine(SwapTiles(tile1, tile2)); // ��������
        }
    }
    bool AreAdjacent(Tile tile1, Tile tile2)
    {
        // ������������Ƿ�������������
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
                selectedTile = targetTile; // ����ѡ�еķ���
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
        // ���� selectedValue �����ķ�����������
        Debug.Log("Selected Value: " + selectedValue);
        tileCount = selectedValue;
    }
}