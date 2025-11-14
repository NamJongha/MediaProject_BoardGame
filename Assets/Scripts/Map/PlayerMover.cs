using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMover : MonoBehaviour
{
    [Header("이동 설정")]
    private float moveSpeed = 4f;
    private float arriveHeightOffset = 1.2f;   // 보드 타일 높이 보정

    [Header("노드 정보")]
    private BoardNode currentNode;
    private bool isMoving = false;

    [Header("브랜치 UI")]
    private BranchSelectorUI branchSelectorUI;   // 갈림길 선택 UI

    private void Start()
    {
        // 씬에서 BranchSelectorUI 자동 찾기
        if (branchSelectorUI == null)
            branchSelectorUI = FindFirstObjectByType<BranchSelectorUI>();
    }
    
    public IEnumerator MoveStepsAndFinishTurn(int steps, Player player, TurnManager turnManager)
    {
        yield return StartCoroutine(MoveSteps(steps, player));   // 이동 수행

        // 이동 종료 후 턴 종료 처리
        turnManager.OnPlayerEndTurn(player.Object.InputAuthority);
    }
    
    public IEnumerator MoveSteps(int steps, Player player)
    {
        if (currentNode == null)
        {
            Debug.LogError("currentNode가 없습니다. 플레이어 스폰 상태 확인 필요.");
            yield break;
        }

        isMoving = true;
        BoardNode finalNode = currentNode;
        BoardNode startNode = BoardGenerator.Instance.GetStartNode();
        if (finalNode != startNode)
        {
            // 첫 칸은 StartNode로 고정 이동
            steps -= 1;
            yield return StartCoroutine(MoveToNode(startNode, triggerEventOnArrival: false));
            finalNode = startNode;

            if (steps <= 0)
            {
                yield return StartCoroutine(MoveToNode(finalNode, triggerEventOnArrival: true));
                currentNode = finalNode;
                isMoving = false;
                yield break;
            }
        }
        
        for (int i = 0; i < steps; i++)
        {
            // 다음 노드가 없으면 중단
            if (finalNode.nextNodes == null || finalNode.nextNodes.Count == 0)
                break;

            // ---- 브랜치 발생 시 UI 선택 대기 ----
            if (finalNode.nextNodes.Count > 1)
            {
                // 🔥 여기 수정됨: Player 전달
                yield return StartCoroutine(ChoosePath(finalNode, player));
            }

            BoardNode nextNode = finalNode.nextNodes[0];
            yield return StartCoroutine(MoveToNode(nextNode, triggerEventOnArrival: false));
            finalNode = nextNode;
        }

        // 마지막 노드에서 이벤트 실행
        yield return StartCoroutine(MoveToNode(finalNode, triggerEventOnArrival: true));

        SetCurrentNode(finalNode);
        isMoving = false;
    }


    private IEnumerator ChoosePath(BoardNode node, Player player)
    {
        // InputAuthority만 UI 띄운다
        branchSelectorUI.Show(node.nextNodes, player);

        // Host(StateAuthority)가 branchSelected = true 로 변경할 때까지 대기
        while (!player.branchSelected)
            yield return null;

        // 선택 완료 → Host에서 해당 브랜치 확정
        BoardNode chosen = branchSelectorUI.GetChosenNode(player);

        node.nextNodes.Clear();
        node.nextNodes.Add(chosen);

        // 다음 선택을 위해 초기화
        player.branchSelected = false;
    }
    
    private IEnumerator MoveToNode(BoardNode targetNode, bool triggerEventOnArrival)
    {
        Vector3 start = transform.position;
        Vector3 end = targetNode.transform.position + new Vector3(0, arriveHeightOffset, 0);

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        // SpecialTile, EventTile 처리
        if (triggerEventOnArrival)
        {
            SpecialTile tile = targetNode.GetComponent<SpecialTile>();
            if (tile != null && tile.effect != null)
            {
                // ScriptableObject 기반 SpecialTile 효과 실행
                tile.effect.ApplyEffect(GetComponent<Player>());
            }
        }
    }

    public BoardNode GetCurrentNode()
    {
        return currentNode;
    }
    
    public void SetCurrentNode(BoardNode node)
    {
        if (node == null)
        {
            Debug.LogError("[PlayerMover] SetCurrentNode()에 null 전달됨!");
            return;
        }

        Debug.Log($"[PlayerMover] 노드 변경: {currentNode} → {node}");
        currentNode = node;
    }

}
