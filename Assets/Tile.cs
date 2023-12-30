using System.Collections;
using UnityEngine;
public class Tile : MonoBehaviour
{
    // 假设您有一个枚举来定义不同的特殊方块类型
    public enum SpecialType { None, horizontal, vertical, bomb }
    public SpecialType specialType = SpecialType.None;
    public enum TileType { Blue, Yellow, Green, Red, Orange,Null }
    public TileType tileType;
    public enum TileState { Idle, Moving, Clearing, Checking }
    public TileState currentState = TileState.Idle;
    public GridManager gridManager;
    public int xIndex;
    public int yIndex;
    public int fallDistance = 0;
    private Animator animator;
    private GameObject lineObj = null;
    private GameObject burstObj = null;
    void Awake()
    {
        animator = GetComponent<Animator>();
        gridManager = transform.parent.GetComponent<GridManager>();
        lineObj = transform.Find("line").gameObject;
        burstObj = transform.Find("burst").gameObject;
    }
    public void Init(int x, int y, TileType type)
    {
        if (tileType == TileType.Null)
        {
            return;
        }
        xIndex = x;
        yIndex = y;
        tileType = type;
        // 设置方块的名字为其坐标
        name = "Tile (" + xIndex + ", " + yIndex + ")";
    }
    public void StartFalling(int distance)
    {
        if(fallDistance != 0)
        {
            fallDistance += 1;
            return;
        }
        fallDistance = distance;
        SetState(TileState.Moving);
        StartCoroutine(FallToPosition());
    }

    IEnumerator FallToPosition()
    {
        Vector3 start = transform.localPosition;
        Vector3 end = new Vector3(xIndex * GridManager.tileSize, -yIndex * GridManager.tileSize, transform.position.z);
        float duration = 0.1f * fallDistance; // 调整下落速度
        float elapsed = 0;

        while (elapsed < duration)
        {
            end = new Vector3(xIndex * GridManager.tileSize, -yIndex * GridManager.tileSize, transform.position.z);//下落过程中可能会需要再次下落更多，所以这里再次设置一下
            elapsed += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(start, end, elapsed / duration);
            yield return null;
        }

        transform.localPosition = end;
        SetState(TileState.Idle);
        fallDistance = 0;
        // 下落完成后检查匹配
        CheckForMatchAfterFalling();
    }
    private void CheckForMatchAfterFalling()
    {
        // 调用 GridManager 或其他相关组件进行匹配检查
        // 例如: gridManager.CheckForMatchesAt(xIndex, yIndex);
        gridManager.CheckForMatchesAt(xIndex, yIndex);
    }

    public void SetState(TileState newState)
    {
        if (currentState == TileState.Clearing)
            return;
        currentState = newState;
        UpdateAnimationState();
    }

    private void UpdateAnimationState()
    {
        if (animator == null) return;

        switch (currentState)
        {
            case TileState.Idle:
                if(specialType == SpecialType.None)
                    animator.Play("idle");
                break;
            case TileState.Moving:
            case TileState.Checking:
                if (specialType == SpecialType.None)
                    animator.Play("move");
                break;
            case TileState.Clearing:
                animator.Play("clear");
                tileType = TileType.Null;
                break;
        }
    }

    // 设置为特殊方块的方法
    public void SetAsSpecialTile(SpecialType type)
    {
        if (tileType == TileType.Null)
        {
            return;
        }
        gridManager.AddScore(5);
        lineObj.SetActive(false);
        burstObj.SetActive(false); //设置为特殊方块前就已经是特殊方块时，它的特殊方块特效需要隐藏一下
        specialType = type;
        switch (type) 
        { 
            case SpecialType.horizontal:
                animator.Play("horizontal");
                break;
            case SpecialType.vertical:
                animator.Play("vertical");
                break;
            case SpecialType.bomb:
                animator.Play("bomb");
                break;
        }
        // 根据特殊类型更改方块的外观或行为
        // 例如，更改方块的颜色、添加特效等
        // ...
    }
    public void PlaySpecialAnim()
    {
        switch (specialType)
        {
            case SpecialType.horizontal:
                lineObj.SetActive(true);
                break;
            case SpecialType.vertical:
                lineObj.SetActive(true);
                lineObj.transform.eulerAngles = new Vector3(0, 0, 90);
                break;
            case SpecialType.bomb:
                burstObj.SetActive(true);
                break;
        }
        specialType = SpecialType.None;
    }
    // 其他逻辑
}