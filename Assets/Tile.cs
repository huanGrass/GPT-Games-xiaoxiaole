using System.Collections;
using UnityEngine;
public class Tile : MonoBehaviour
{
    // ��������һ��ö�������岻ͬ�����ⷽ������
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
        // ���÷��������Ϊ������
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
        float duration = 0.1f * fallDistance; // ���������ٶ�
        float elapsed = 0;

        while (elapsed < duration)
        {
            end = new Vector3(xIndex * GridManager.tileSize, -yIndex * GridManager.tileSize, transform.position.z);//��������п��ܻ���Ҫ�ٴ�������࣬���������ٴ�����һ��
            elapsed += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(start, end, elapsed / duration);
            yield return null;
        }

        transform.localPosition = end;
        SetState(TileState.Idle);
        fallDistance = 0;
        // ������ɺ���ƥ��
        CheckForMatchAfterFalling();
    }
    private void CheckForMatchAfterFalling()
    {
        // ���� GridManager ����������������ƥ����
        // ����: gridManager.CheckForMatchesAt(xIndex, yIndex);
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

    // ����Ϊ���ⷽ��ķ���
    public void SetAsSpecialTile(SpecialType type)
    {
        if (tileType == TileType.Null)
        {
            return;
        }
        gridManager.AddScore(5);
        lineObj.SetActive(false);
        burstObj.SetActive(false); //����Ϊ���ⷽ��ǰ���Ѿ������ⷽ��ʱ���������ⷽ����Ч��Ҫ����һ��
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
        // �����������͸��ķ������ۻ���Ϊ
        // ���磬���ķ������ɫ�������Ч��
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
    // �����߼�
}