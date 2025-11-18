using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class PlayerMover : NetworkBehaviour
{
    [Header("이동 설정")]
    private float moveSpeed = 4f;
    private float arriveHeightOffset = 1.2f;   // 보드 타일 높이 보정

    [Header("노드 정보")]
    private BoardNode currentNode;
    private bool isMoving = false;

    [Header("브랜치 UI")]
    private BranchSelectorUI branchSelectorUI;   // 갈림길 선택 UI
    
    private NetworkCharacterController _cc;

    private void Start()
    {
        // 씬에서 BranchSelectorUI 자동 찾기
        if (branchSelectorUI == null)
            branchSelectorUI = FindFirstObjectByType<BranchSelectorUI>();
        _cc = GetComponent<NetworkCharacterController>();
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
            if (player.currentNodeId >= 0)
            {
                currentNode = BoardGenerator.Instance.GetNodeById(player.currentNodeId);
                if (currentNode == null)
                {
                    Debug.LogError($"[PlayerMover] NodeId {player.currentNodeId} 를 찾을 수 없습니다.");
                    yield break;
                }
            }
            else
            {
                // 아직 보드 위에 없으면 StartNode로 초기화
                currentNode = BoardGenerator.Instance.GetStartNode();
                if (currentNode == null)
                {
                    Debug.LogError("[PlayerMover] StartNode가 없습니다.");
                    yield break;
                }
            }

            // 위치도 보정
            TeleportToNode(currentNode);
        }

        isMoving = true;
        BoardNode finalNode = GetCurrentNode();
        
        for (int i = 0; i < steps; i++)
        {
            // 다음 노드가 없으면 중단
            if (finalNode.nextNodes == null || finalNode.nextNodes.Count == 0)
            {
                Debug.Log($"[PlayerMover] No more nodes to move. Stopping at final node: {finalNode.id}");
                break;
            }

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

        if (finalNode.nextNodes == null || finalNode.nextNodes.Count == 0)
        {
            LogManager.Instance.Log($"[PlayerMover] Final node reached! Player={player.name}, Node ID={finalNode.id}");
        }
        
        SetCurrentNode(finalNode);
        player.currentNodeId = finalNode.id;
        
        Debug.Log($"PlayerMover → player.currentNodeId updated to {finalNode.id}");
        
        isMoving = false;
    }


    private IEnumerator ChoosePath(BoardNode node, Player player)
    {
        if (branchSelectorUI == null)
        {
            Debug.LogError("[PlayerMover] branchSelectorUI is null! Make sure BranchSelectorUI exists in the scene.");
            yield break;
        }

        if (node.nextNodes == null || node.nextNodes.Count == 0)
        {
            Debug.LogWarning("[PlayerMover] No branches to choose from. Skipping branch selection.");
            yield break;
        }

        // InputAuthority만 UI 띄운다
        branchSelectorUI.Show(node.nextNodes, player);

        // Host(StateAuthority)가 branchSelected = true 로 변경할 때까지 대기
        while (!player.branchSelected)
            yield return null;

        BoardNode chosen = branchSelectorUI.GetChosenNode(player);

        if (chosen == null)
        {
            Debug.LogError("[PlayerMover] chosen branch is null.");
            yield break;
        }

        node.nextNodes.Clear();

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
            TeleportPosition(Vector3.Lerp(start, end, t));
            yield return null;
        }

        // SpecialTile, EventTile 처리
        if (triggerEventOnArrival)
        {
            SpecialTile tile = targetNode.GetComponent<SpecialTile>();
            if (tile != null)
            {
                if (tile.effect != null)
                {
                    tile.effect.ApplyEffect(GetComponent<Player>());
                }
                else
                {
                    Debug.Log($"[SpecialTile] Tile {tile.name} has NO effect, skipping event.");
                }
            }
        }
        Physics.SyncTransforms();
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

        Debug.Log($"[PlayerMover] 노드 변경: {(currentNode != null ? currentNode.id : -1)} → {node.id}");
        currentNode = node;
    }
    
    private void TeleportPosition(Vector3 position)
    {
        if (_cc)
            _cc.Teleport(position);
        else
            transform.position = position;
    }

// Node 기준 위치 이동
    private void TeleportToNode(BoardNode node)
    {
        Vector3 pos = node.transform.position + new Vector3(0, arriveHeightOffset, 0);
        TeleportPosition(pos);
    }

}
