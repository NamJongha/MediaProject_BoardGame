using System.Collections.Generic;
using UnityEngine;

public class SpecialTileGenerator : MonoBehaviour
{
    private int safeStart = 3;
    private int safeEnd = 3;

    [Header("특수 타일 프리팹")]
    public GameObject[] specialTilePrefabs;

    public void GenerateSpecialTiles(BoardGenerator board)
    {
        List<BoardNode> allNodes = board.GetAllNodes();
        int total = allNodes.Count;

        List<BoardNode> nodesToReplace = new List<BoardNode>();

        foreach (var region in board.terrainRegions)
        {
            int placed = 0;

            // 지형별 노드 선택 (시작/도착 안전 거리 제외)
            List<BoardNode> regionNodes = allNodes.FindAll(n =>
                n.terrainType == region.terrainType &&
                n.nodeType != NodeType.Goal &&
                n.id >= safeStart &&
                n.id < total - safeEnd);

            // 브랜치 노드 포함
            regionNodes.AddRange(allNodes.FindAll(n =>
                n.terrainType == region.terrainType && n.isBranchNode));

            while (placed < region.specialTileCount && regionNodes.Count > 0)
            {
                int idx = Random.Range(0, regionNodes.Count);
                BoardNode node = regionNodes[idx];
                regionNodes.RemoveAt(idx);

                if (node == null) continue;
                nodesToReplace.Add(node);
                placed++;
            }
        }

        // 🔹 실제 교체 처리 (기존 로직 유지)
        foreach (var node in nodesToReplace)
        {
            if (node == null || node.transform == null)
                continue;

            Vector3 pos = node.transform.position;
            Quaternion rot = node.transform.rotation;
            Transform parent = node.transform.parent;
            TerrainType terrain = node.terrainType;

            // 🔹 새 방식: Inspector에서 ScriptableObject가 연결된 프리팹을 사용
            if (specialTilePrefabs == null || specialTilePrefabs.Length == 0)
            {
                Debug.LogWarning("특수 타일 프리팹이 연결되지 않았습니다!");
                continue;
            }

            GameObject prefab = specialTilePrefabs[Random.Range(0, specialTilePrefabs.Length)];
            GameObject specialObj = Instantiate(prefab, pos, rot, parent);

            // 기존 SpecialTile 구성 확인
            SpecialTile special = specialObj.GetComponent<SpecialTile>();
            if (special == null)
                special = specialObj.AddComponent<SpecialTile>();

            // BoardNode 교체 로직 (기존 유지)
            BoardNode newNode = specialObj.GetComponent<BoardNode>();
            if (newNode == null)
                newNode = specialObj.AddComponent<BoardNode>();

            newNode.id = node.id;
            newNode.terrainType = terrain;
            newNode.nodeType = NodeType.Special;
            newNode.isBranchNode = node.isBranchNode;
            newNode.nextNodes = new List<BoardNode>(node.nextNodes);

            // 이전 노드 연결 업데이트
            foreach (var prev in allNodes)
            {
                if (prev == null || prev.nextNodes == null) continue;

                for (int i = 0; i < prev.nextNodes.Count; i++)
                {
                    if (prev.nextNodes[i] == node)
                        prev.nextNodes[i] = newNode;
                }
            }

            int index = allNodes.IndexOf(node);
            if (index >= 0)
                allNodes[index] = newNode;

            Destroy(node.gameObject);
        }

        Debug.Log($"특수 타일 교체 완료: {nodesToReplace.Count}개 노드 변경됨");
    }
}
