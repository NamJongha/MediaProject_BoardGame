using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathGenerator : MonoBehaviour
{
    // =======================================================================
    //  메인 루트 생성
    // =======================================================================
    public void GenerateMainPath(BoardGenerator board, List<BoardNode> allNodes, float tileSize)
    {
        Vector2 direction = board.endCoord - board.startCoord;
        float stepX = direction.x / board.mainPathLength;

        float curveAmp = Random.Range(6f, 10f);
        float curveFreq = Random.Range(0.15f, 0.25f);

        BoardNode prev = null;

        for (int i = 0; i < board.mainPathLength; i++)
        {
            float offsetZ = Mathf.Sin(i * curveFreq) * curveAmp;

            Vector3 pos = new Vector3(
                board.startCoord.x + i * stepX * board.spacingMultiplier,
                0,
                board.startCoord.y + offsetZ
            );

            if (pos.x >= board.endCoord.x)
                break;

            TerrainType terrain = board.GetRegionByPosition(pos);
            GameObject prefab = board.GetTilePrefab(terrain) ?? board.defaultNodePrefab;

            GameObject tile = Instantiate(prefab, pos, Quaternion.identity, board.tileParent);
            BoardNode node = tile.GetComponent<BoardNode>() ?? tile.AddComponent<BoardNode>();

            node.id = allNodes.Count;
            node.terrainType = terrain;
            node.nodeType = NodeType.Normal;

            allNodes.Add(node);

            if (prev != null)
                prev.nextNodes.Add(node);

            prev = node;
        }

        // Goal Node 생성
        Vector3 goalPos = new Vector3(board.endCoord.x, 0, board.endCoord.y);
        GameObject goalTile = Instantiate(board.defaultNodePrefab, goalPos, Quaternion.identity, board.tileParent);

        BoardNode goalNode = goalTile.GetComponent<BoardNode>() ?? goalTile.AddComponent<BoardNode>();
        goalNode.nodeType = NodeType.Goal;

        goalTile.GetComponent<Renderer>().material.color = Color.red;

        prev.nextNodes.Add(goalNode);
        allNodes.Add(goalNode);

        Debug.Log($"[1PASS] Main Path 생성 완료: {allNodes.Count}개");
    }

    // =======================================================================
    //  브랜치 생성 — 이전 브랜치 종착점 기준 4~5칸 뒤 시작
    // =======================================================================
    public void GenerateBranchesSync(BoardGenerator board, List<BoardNode> allNodes, float tileSize)
    {
        int total = allNodes.Count;
        if (total < 20)
            return;

        Debug.Log($"[2PASS:SYNC] 브랜치 생성 시작 (총 {total} 노드)");

        int nextAvailableStartIndex = 4;

        for (int i = 4; i < total - 12; i++)
        {
            // 다음 브랜치를 시작할 수 있는 최소 인덱스
            if (i < nextAvailableStartIndex)
                continue;

            // 확률 체크
            if (Random.value > board.branchChance)
                continue;

            BoardNode startNode = allNodes[i];

            // 도착 노드는 5~10칸 뒤
            int targetIndex = Mathf.Min(total - 1, i + Random.Range(5, 11));
            BoardNode targetNode = allNodes[targetIndex];

            Debug.Log($"[Branch:SYNC] {i} → {targetIndex}");

            // 동기화된 ARC 브랜치 생성
            CreateBranchArcSync(board, allNodes, startNode, targetNode, tileSize);

            // 다음 브랜치 가능 시작점
            nextAvailableStartIndex = targetIndex + Random.Range(4, 6);
        }

        Debug.Log("[2PASS:SYNC] 브랜치 생성 완료");
    }


// =======================================================================
//  ARC 베지어 브랜치 생성 (완전 동기 버전)
// =======================================================================
    private void CreateBranchArcSync(BoardGenerator board, List<BoardNode> allNodes,
        BoardNode startNode, BoardNode endNode, float tileSize)
    {
        Vector3 A = startNode.transform.position;
        Vector3 B = endNode.transform.position;

        float dist = Vector3.Distance(A, B);
        if (dist < tileSize * 3f)
            return;

        Vector3 dir = (B - A).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, dir);

        float side = Random.value < 0.5f ? 1f : -1f;

        Vector3 mid = (A + B) * 0.5f;
        float arcHeight = dist * 0.35f;
        Vector3 P = mid + right * arcHeight * side;

        int segments = Mathf.Max(6, Mathf.FloorToInt(dist / tileSize * 1.2f));

        BoardNode prev = startNode;

        for (int j = 1; j < segments; j++)
        {
            float t = j / (float)segments;

            Vector3 pos =
                (1 - t) * (1 - t) * A +
                2 * (1 - t) * t * P +
                t * t * B;

            TerrainType terrain = board.GetRegionByPosition(pos);
            GameObject prefab = board.GetTilePrefab(terrain) ?? board.defaultNodePrefab;

            GameObject tile = GameObject.Instantiate(prefab, pos, Quaternion.identity, board.tileParent);
            BoardNode node = tile.GetComponent<BoardNode>() ?? tile.AddComponent<BoardNode>();

            node.id = allNodes.Count;
            node.nodeType = NodeType.Branch;
            node.isBranchNode = true;
            node.terrainType = terrain;

            prev.nextNodes.Add(node);
            allNodes.Add(node);

            prev = node;
        }

        prev.nextNodes.Add(endNode);
    }
}
