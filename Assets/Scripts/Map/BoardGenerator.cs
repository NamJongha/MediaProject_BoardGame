using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RegionArea
{
    public string name;
    public TerrainType terrainType;
    public RectInt area;
    public int specialTileCount;
}

public class BoardGenerator : MonoBehaviour
{
    [Header("Prefab Settings")]
    public GameObject nodePrefab;
    public Transform parent;

    [Header("Special Tile Prefabs")]
    public GameObject staminaDownTilePrefab;
    public GameObject itemTilePrefab;
    public GameObject staminaUpTilePrefab;

    [Header("Path Settings")]
    public int mainPathLength = 60;
    public Vector2Int startCoord = new Vector2Int(0, 0);
    public Vector2Int endCoord = new Vector2Int(200, 0);
    [Range(0.5f, 2f)] public float spacingMultiplier = 1.1f;

    [Header("Branch Settings")]
    [Range(0f, 1f)] public float branchChance = 0.2f;
    public float branchInterval = 8f;
    public float minDistanceBetweenBranches = 2.5f;
    [Range(40f, 80f)] public float branchAngleRange = 60f;

    [Header("Region Definitions")]
    public List<RegionArea> regions = new List<RegionArea>();

    private List<BoardNode> allNodes = new List<BoardNode>();
    private BoardNode goalNode;
    private int lastBranchIndex = -999;

    // 맵 생성 완료 시 호출될 이벤트
    public delegate void BoardGeneratedHandler();
    public event BoardGeneratedHandler OnBoardGenerated;


    void Start()
    {
        GenerateBoard();
    }

    public void GenerateBoard()
    {
        if (parent != null)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                DestroyImmediate(parent.GetChild(i).gameObject);
        }

        allNodes.Clear();
        float tileSize = GetTileSize(nodePrefab);

        List<BoardNode> mainPath = CreateMainPath(tileSize);

        // Goal 노드 생성
        BoardNode lastNode = mainPath[mainPath.Count - 1];
        Vector3 goalPos = new Vector3(endCoord.x, 0, endCoord.y);
        GameObject goalObj = Instantiate(nodePrefab, goalPos, Quaternion.identity, parent);
        goalNode = goalObj.GetComponent<BoardNode>();
        goalNode.nodeType = NodeType.Goal;
        goalObj.GetComponent<Renderer>().material.color = Color.red;
        lastNode.nextNodes.Add(goalNode);
        allNodes.Add(goalNode);

        // 브랜치 완료 후 특수 타일 생성 (지연 실행)
        StartCoroutine(WaitAndGenerateSpecialTiles());

        Debug.Log($"맵 생성 완료. 전체 노드 수: {allNodes.Count}");
    }

    IEnumerator WaitAndGenerateSpecialTiles()
    {
        // 코루틴들이 끝날 때까지 잠시 대기
        yield return new WaitForSeconds(0.6f);
        GenerateSpecialTiles();

        OnBoardGenerated?.Invoke(); // 플레이어 등에게 "맵 생성 완료" 알림
    }

    List<BoardNode> CreateMainPath(float tileSize)
    {
        List<BoardNode> path = new List<BoardNode>();
        Vector2 direction = (endCoord - startCoord);
        float stepX = direction.x / mainPathLength;
        float stepZ = direction.y / mainPathLength;

        Vector3 lastPos = new Vector3(startCoord.x, 0, startCoord.y);
        BoardNode previous = null;

        float curveAmp = Random.Range(3f, 8f);
        float curveFreq = Random.Range(0.15f, 0.35f);

        for (int i = 0; i < mainPathLength; i++)
        {
            float offset = Mathf.Sin(i * curveFreq) * curveAmp + Random.Range(-0.3f, 0.3f);
            Vector3 pos = lastPos + new Vector3(stepX, 0, offset + stepZ);

            TerrainType regionType = GetRegionByPosition(pos);
            GameObject prefab = GetTilePrefab(regionType);
            if (prefab == null) prefab = nodePrefab;

            GameObject tile = Instantiate(prefab, pos, Quaternion.identity, parent);
            BoardNode node = tile.GetComponent<BoardNode>();
            node.id = allNodes.Count;
            node.terrainType = regionType;
            node.nodeType = NodeType.Normal;

            if (previous != null)
                previous.nextNodes.Add(node);

            allNodes.Add(node);
            path.Add(node);
            previous = node;
            lastPos = pos;

            if (i - lastBranchIndex >= branchInterval && Random.value < branchChance && i < mainPathLength - 12)
            {
                int targetIndex = Mathf.Min(i + Random.Range(6, 10), mainPathLength - 1);
                StartCoroutine(CreateBranchAfterDelay(node, targetIndex, tileSize));
                lastBranchIndex = i;
            }
        }

        return path;
    }

    IEnumerator CreateBranchAfterDelay(BoardNode startNode, int targetIndex, float tileSize)
    {
        yield return null;

        if (targetIndex < allNodes.Count)
        {
            BoardNode targetNode = allNodes[targetIndex];
            CreateBezierBranch(startNode, targetNode, tileSize);
        }
    }

    void CreateBezierBranch(BoardNode startNode, BoardNode targetNode, float tileSize)
    {
        // 안전 검사
        if (startNode == null || targetNode == null)
        {
            Debug.LogWarning("브랜치 생성 중 노드가 이미 삭제됨 — 생략됨");
            return;
        }

        if (startNode.transform == null || targetNode.transform == null)
        {
            Debug.LogWarning("삭제된 노드 접근으로 브랜치 생략됨");
            return;
        }

        float step = tileSize * spacingMultiplier;
        bool goLeft = Random.value < 0.5f;

        Vector3 startPos = startNode.transform.position;
        Vector3 targetPos = targetNode.transform.position;
        Vector3 mainDir = (targetPos - startPos).normalized;

        float randomAngle = Random.Range(branchAngleRange - 10f, branchAngleRange + 10f);
        Vector3 branchDir = Quaternion.Euler(0, goLeft ? -randomAngle : randomAngle, 0) * mainDir;
        Vector3 controlPoint = startPos + branchDir * (Vector3.Distance(startPos, targetPos) * 0.6f);

        foreach (var n in allNodes)
        {
            if (Vector3.Distance(controlPoint, n.transform.position) < minDistanceBetweenBranches * 2f)
                return;
        }

        int segmentCount = Mathf.Max(8, Mathf.FloorToInt(Vector3.Distance(startPos, targetPos) / step));
        BoardNode prev = startNode;

        for (int i = 1; i <= segmentCount; i++)
        {
            float t = i / (float)segmentCount;
            Vector3 pos =
                Mathf.Pow(1 - t, 2) * startPos +
                2 * (1 - t) * t * controlPoint +
                Mathf.Pow(t, 2) * targetPos;

            bool overlap = false;
            foreach (var n in allNodes)
            {
                if (Vector3.Distance(pos, n.transform.position) < minDistanceBetweenBranches)
                {
                    overlap = true;
                    break;
                }
            }
            if (overlap) continue;

            TerrainType regionType = GetRegionByPosition(pos);
            GameObject prefab = GetTilePrefab(regionType);
            if (prefab == null) prefab = nodePrefab;

            GameObject tile = Instantiate(prefab, pos, Quaternion.identity, parent);
            BoardNode node = tile.GetComponent<BoardNode>();
            node.id = allNodes.Count;
            node.terrainType = regionType;
            node.nodeType = NodeType.Branch;
            node.isBranchNode = true;

            prev.nextNodes.Add(node);
            allNodes.Add(node);
            prev = node;
        }

        prev.nextNodes.Add(targetNode);
    }

    void GenerateSpecialTiles()
    {
        int safeStart = 3;
        int safeEnd = 3;
        int totalNodes = allNodes.Count;
        List<BoardNode> nodesToReplace = new List<BoardNode>();

        foreach (var region in regions)
        {
            int placed = 0;

            List<BoardNode> regionNodes = allNodes.FindAll(n =>
                n.terrainType == region.terrainType &&
                n.nodeType != NodeType.Goal &&
                n.id >= safeStart &&
                n.id < totalNodes - safeEnd);

            regionNodes.AddRange(allNodes.FindAll(n =>
                n.terrainType == region.terrainType && n.isBranchNode));

            while (placed < region.specialTileCount && regionNodes.Count > 0)
            {
                int idx = Random.Range(0, regionNodes.Count);
                BoardNode node = regionNodes[idx];
                regionNodes.RemoveAt(idx);
                nodesToReplace.Add(node);
                placed++;
            }
        }

        foreach (var node in nodesToReplace)
        {
            if (node == null) continue;
            if (node.transform == null) continue;

            Vector3 pos = node.transform.position;
            Quaternion rot = node.transform.rotation;
            Transform parentTransform = node.transform.parent;
            TerrainType terrain = node.terrainType;

            EventType eventType = (EventType)Random.Range(1, 4);
            GameObject prefab = eventType switch
            {
                EventType.StaminaDown => staminaDownTilePrefab,
                EventType.ItemGet => itemTilePrefab,
                EventType.StaminaUp => staminaUpTilePrefab,
                _ => nodePrefab
            };

            if (prefab == null) continue;

            GameObject special = Instantiate(prefab, pos, rot, parentTransform);
            var specialEvent = special.AddComponent<SpecialTile>();
            specialEvent.eventType = eventType;

            BoardNode newNode = special.AddComponent<BoardNode>();
            newNode.id = node.id;
            newNode.terrainType = terrain;
            newNode.nodeType = node.nodeType;
            newNode.isBranchNode = node.isBranchNode;
            newNode.nextNodes = new List<BoardNode>(node.nextNodes);

            foreach (var prev in allNodes)
            {
                for (int i = 0; i < prev.nextNodes.Count; i++)
                {
                    if (prev.nextNodes[i] == node)
                        prev.nextNodes[i] = newNode;
                }
            }

            int index = allNodes.IndexOf(node);
            if (index >= 0) allNodes[index] = newNode;

            Destroy(node.gameObject);
        }

        Debug.Log("특수 타일 교체 완료 (브랜치 및 경로 유지)");
    }

    TerrainType GetRegionByPosition(Vector3 pos)
    {
        foreach (var region in regions)
        {
            if (pos.x >= region.area.x && pos.x <= region.area.x + region.area.width)
                return region.terrainType;
        }
        return TerrainType.Forest;
    }

    GameObject GetTilePrefab(TerrainType type)
    {
        return nodePrefab;
    }

    float GetTileSize(GameObject prefab)
    {
        if (prefab == null) return 1f;
        Renderer r = prefab.GetComponentInChildren<Renderer>();
        if (r != null) return r.bounds.size.x;
        return 1f;
    }

    public BoardNode GetStartNode()
    {
        if (allNodes == null || allNodes.Count == 0)
            return null;
        return allNodes[0];
    }


}
