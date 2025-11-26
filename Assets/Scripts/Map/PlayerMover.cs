using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;

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
            // goal 체크
            if (finalNode.nodeType == NodeType.Goal)
            {
                LogManager.Instance.Log($"[PlayerMover] Goal reached at Node {finalNode.id}, stopping.");
                GameManager.Instance.RequestShowGameOverUI(player.name);

                SetCurrentNode(finalNode);
                player.currentNodeId = finalNode.id;
                isMoving = false;
                break;
            }
            
            // destroy된 노드 제거
            if (finalNode.nextNodes != null)
            {
                finalNode.nextNodes = finalNode.nextNodes
                    .Where(n => n != null)
                    .ToList();
            }

            // 노드 없음
            if (finalNode.nextNodes == null || finalNode.nextNodes.Count == 0)
            {
                Debug.Log($"[PlayerMover] No valid next nodes at {finalNode.id}");
                break;
            }

            bool nextIsGoal = finalNode.nextNodes.Exists(n => n != null && n.nodeType == NodeType.Goal);
            
            BoardNode nextNode;

            // 갈림길인 경우에만 플레이어에게 선택권 부여
            if (finalNode.nextNodes.Count > 1 && !nextIsGoal)
            {
                // 선택 UI + RPC 대기
                yield return StartCoroutine(ChoosePath(finalNode, player));

                // 서버에서 결정된 chosenBranchIndex 사용
                int index = player.chosenBranchIndex;
                if (index < 0 || index >= finalNode.nextNodes.Count)
                {
                    Debug.LogError($"[PlayerMover] invalid chosenBranchIndex {index} at node {finalNode.id}");
                    yield break;
                }

                nextNode = finalNode.nextNodes[index];
            }
            else
            {
                // 갈림길이 아니면 첫 번째 노드로 진행
                nextNode = finalNode.nextNodes[0];
            }
            
            yield return StartCoroutine(MoveToNode(nextNode, false));
            finalNode = nextNode;
        }


        // 마지막 노드에서 이벤트 실행
        yield return StartCoroutine(
            MoveToNode(finalNode, triggerEventOnArrival: true)
        );

        if (finalNode.nodeType == NodeType.Goal)
        {
            Debug.Log("[PlayerMover] Goal event executed. Movement fully completed.");
            SetCurrentNode(finalNode);
            player.currentNodeId = finalNode.id;
            isMoving = false;
            yield break;
        }

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
            Debug.LogError("[PlayerMover] branchSelectorUI is null!");
            yield break;
        }

        if (node.nextNodes == null || node.nextNodes.Count == 0)
        {
            Debug.LogWarning("[PlayerMover] No branches found.");
            yield break;
        }

        // (1) 서버(StateAuthority) → 지금 턴인 플레이어(InputAuthority)에게 "UI 열어라" RPC 전달
        if (Object.HasStateAuthority)
        {
            int[] branchIds = node.nextNodes
                .Where(n => n != null)
                .Select(n => n.id)
                .ToArray();

            player.RPC_OpenBranchSelector(branchIds);
        }

        // (2) 클라이언트는 UI에서 버튼 클릭 → RPC_ChooseBranch(index) 실행
        while (!player.branchSelected)
            yield return null;

        // (3) 서버가 최종 선택 확정
        if (Object.HasStateAuthority)
        {
            player.branchSelected = false;
        }
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
