using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerPathMover : MonoBehaviour
{
    public float moveSpeed = 3f;
    public BoardGenerator boardGenerator;
    public Text debugText;

    private BoardNode currentNode;
    private bool isMoving = false;

    void Awake()
    {
        if (boardGenerator == null)
            boardGenerator = FindObjectOfType<BoardGenerator>();

        // 이벤트 이름 변경
        boardGenerator.OnBoardGenerated += MoveToStartAfterGeneration;
    }

    void OnDestroy()
    {
        boardGenerator.OnBoardGenerated -= MoveToStartAfterGeneration;
    }

    private void MoveToStartAfterGeneration()
    {
        StartCoroutine(MoveToStartCoroutine());
    }

    private IEnumerator MoveToStartCoroutine()
    {
        yield return null;
        currentNode = boardGenerator.GetStartNode();
        if (currentNode != null)
        {
            transform.position = currentNode.transform.position + Vector3.up * 0.5f;
            ShowDebug("맵 생성 완료 후 시작 노드에 플레이어 배치 완료!");
        }
        else
        {
            Debug.LogError("시작 노드를 찾을 수 없습니다.");
        }
    }

    void Update()
    {
        if (!isMoving)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) StartCoroutine(MoveSteps(1));
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) StartCoroutine(MoveSteps(2));
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) StartCoroutine(MoveSteps(3));
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) StartCoroutine(MoveSteps(4));
            if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) StartCoroutine(MoveSteps(5));
        }
    }

    IEnumerator MoveSteps(int steps)
    {
        if (currentNode == null)
        {
            Debug.LogError("currentNode가 없습니다. 시작 배치가 되었는지 확인하세요.");
            yield break;
        }

        isMoving = true;
        BoardNode finalNode = currentNode;

        for (int i = 0; i < steps; i++)
        {
            if (finalNode.nextNodes == null || finalNode.nextNodes.Count == 0)
            {
                ShowDebug("더 이상 진행할 수 없습니다.");
                break;
            }

            // 갈림길이면 선택
            if (finalNode.nextNodes.Count > 1)
            {
                ShowDebug("갈림길 발견! 방향 선택: [←]갈림길 / [→]원래길");
                yield return StartCoroutine(ChoosePath(finalNode));
            }

            BoardNode nextNode = finalNode.nextNodes[0];
            yield return StartCoroutine(MoveToNode(nextNode, triggerEventOnArrival: false));
            finalNode = nextNode;

            if (finalNode.nextNodes == null || finalNode.nextNodes.Count == 0)
                break;
        }

        // 마지막 노드에서만 이벤트 실행
        yield return StartCoroutine(MoveToNode(finalNode, triggerEventOnArrival: true));
        currentNode = finalNode;
        isMoving = false;
    }

    IEnumerator ChoosePath(BoardNode nodeAtFork)
    {
        bool chosen = false;
        while (!chosen)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                nodeAtFork.nextNodes.Reverse();
                chosen = true;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                chosen = true;
            }
            yield return null;
        }
    }

    IEnumerator MoveToNode(BoardNode targetNode, bool triggerEventOnArrival)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = targetNode.transform.position + Vector3.up * 0.5f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        transform.position = endPos;
        yield return new WaitForSeconds(0.02f);

        if (triggerEventOnArrival)
        {
            SpecialTile tile = targetNode.GetComponent<SpecialTile>();
            if (tile != null)
            {
                tile.TriggerEvent();
            }
        }
    }

    void ShowDebug(string msg)
    {
        if (debugText != null) debugText.text = msg;
        else Debug.Log(msg);
    }
}
