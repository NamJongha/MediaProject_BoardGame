using System.Collections.Generic;
using UnityEngine;

public class BoardGenerator : MonoBehaviour
{
    [Header("Prefab Settings")]
    public GameObject nodePrefab;
    public Transform parent;

    [Header("Tile Prefabs by Terrain")]
    public GameObject forestTilePrefab;
    public GameObject volcanoTilePrefab;
    public GameObject glacierTilePrefab;

    [Header("Path Settings")]
    public int mainPathLength = 25;
    public int branchCount = 3;
    public int minBranchLength = 3;
    public int maxBranchLength = 6;
    [Range(0f, 1f)] public float curveIntensity = 0.4f;

    [Header("Event Node Settings")]
    [Range(0, 100)] public int itemChance = 20;
    [Range(0, 100)] public int eventChance = 15;
    [Range(0, 100)] public int trapChance = 10;
    [Range(0, 100)] public int starChance = 5;

    [Header("Region Settings")]
    public int regionSize = 8;

    [Header("Color Settings")]
    public Color branchColor = new Color(1f, 0.85f, 0.2f, 1f); // 노란색 계열
    public Color mainPathColor = Color.white; // 기본색

    private List<BoardNode> allNodes = new List<BoardNode>();
    private BoardNode goalNode;

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

        // 1️⃣ S자형 오리지널 루트 생성
        List<BoardNode> mainPath = CreateMainPath(tileSize);

        // 2️⃣ Goal 추가
        BoardNode lastNode = mainPath[mainPath.Count - 1];
        Vector3 goalPos = lastNode.transform.position + Vector3.right * tileSize;
        GameObject goalObj = Instantiate(nodePrefab, goalPos, Quaternion.identity, parent);
        goalNode = goalObj.GetComponent<BoardNode>();
        goalNode.nodeType = NodeType.Goal;
        lastNode.nextNodes.Add(goalNode);
        allNodes.Add(goalNode);

        // 3️⃣ 복귀형 갈림길 생성
        CreateBranchPaths(mainPath, tileSize);

        Debug.Log($"✅ 맵 생성 완료! MainPath: {mainPath.Count}, Branches: {branchCount}");
    }

    // 🌀 S자형 루트
    List<BoardNode> CreateMainPath(float tileSize)
    {
        List<BoardNode> path = new List<BoardNode>();
        BoardNode previous = null;

        for (int i = 0; i < mainPathLength; i++)
        {
            float x = i * (tileSize * 0.7f);
            float z = Mathf.Sin(i * 0.5f) * (tileSize * 3f);
            Vector3 curvedPos = new Vector3(x, 0, z);

            GameObject prefab = GetTilePrefab(GetRegionType(i));
            GameObject obj = Instantiate(prefab, curvedPos, Quaternion.identity, parent);

            BoardNode node = obj.GetComponent<BoardNode>();
            node.id = allNodes.Count;
            node.terrainType = GetRegionType(i);
            node.nodeType = NodeType.Normal;
            allNodes.Add(node);
            path.Add(node);

            // 🟩 색상 적용 (메인 루트)
            SetTileColor(obj, mainPathColor);

            if (previous != null)
                previous.nextNodes.Add(node);

            previous = node;
        }

        return path;
    }

    // 🌿 갈림길 생성 (확실히 복귀형)
    void CreateBranchPaths(List<BoardNode> mainPath, float tileSize)
    {
        if (branchCount <= 0) return;
        HashSet<int> used = new HashSet<int>();

        for (int i = 0; i < branchCount; i++)
        {
            int startIndex = Random.Range(3, mainPath.Count - 8);
            if (used.Contains(startIndex)) continue;
            used.Add(startIndex);

            int branchLength = Random.Range(minBranchLength, maxBranchLength + 1);
            BoardNode startNode = mainPath[startIndex];
            Vector3 sideDir = (i % 2 == 0) ? Vector3.forward : Vector3.back;

            BoardNode previous = startNode;

            // 1️⃣ 갈림길 본체
            for (int j = 0; j < branchLength; j++)
            {
                Vector3 pos = startNode.transform.position
                              + sideDir * (tileSize * (j + 1))
                              + Vector3.right * (tileSize * (j * 0.3f));

                GameObject prefab = GetTilePrefab(GetRegionType(startIndex + j));
                GameObject obj = Instantiate(prefab, pos, Quaternion.identity, parent);

                BoardNode node = obj.GetComponent<BoardNode>();
                node.id = allNodes.Count;
                node.terrainType = GetRegionType(startIndex + j);
                node.nodeType = GetSpecialNodeType();
                allNodes.Add(node);

                // 🟨 색상 적용 (갈림길)
                SetTileColor(obj, branchColor);

                previous.nextNodes.Add(node);
                previous = node;
            }

            // 2️⃣ 복귀 경로 추가
            int curveNodes = 3;
            for (int k = 0; k < curveNodes; k++)
            {
                float xOffset = tileSize * (k + 1);
                float zOffset = Mathf.Lerp(sideDir.z * (branchLength * tileSize), 0, (float)k / curveNodes);
                Vector3 pos = previous.transform.position + new Vector3(xOffset, 0, -zOffset * 0.3f);

                GameObject midObj = Instantiate(nodePrefab, pos, Quaternion.identity, parent);
                BoardNode midNode = midObj.GetComponent<BoardNode>();
                midNode.id = allNodes.Count;
                midNode.nodeType = NodeType.Normal;
                allNodes.Add(midNode);

                // 🟨 색상 적용 (복귀 경로도 갈림길 색 유지)
                SetTileColor(midObj, branchColor);

                previous.nextNodes.Add(midNode);
                previous = midNode;
            }

            // 3️⃣ 루트로 복귀
            BoardNode rejoinTarget = FindClosestNodeByDistance(previous.transform.position, mainPath);
            if (rejoinTarget != null)
            {
                previous.nextNodes.Add(rejoinTarget);
            }
        }
    }

    // 🎨 타일 색상 변경
    void SetTileColor(GameObject tile, Color color)
    {
        Renderer renderer = tile.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(renderer.material); // 인스턴스화
            renderer.material.color = color;
        }
    }

    // 📍 가장 가까운 루트 노드 탐색
    BoardNode FindClosestNodeByDistance(Vector3 pos, List<BoardNode> mainPath)
    {
        BoardNode closest = null;
        float minDist = float.MaxValue;

        foreach (BoardNode node in mainPath)
        {
            float dist = Vector3.Distance(pos, node.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = node;
            }
        }

        return (minDist < 15f) ? closest : null;
    }

    NodeType GetSpecialNodeType()
    {
        int roll = Random.Range(0, 100);
        if (roll < starChance) return NodeType.Star;
        if (roll < starChance + itemChance) return NodeType.Item;
        if (roll < starChance + itemChance + eventChance) return NodeType.Event;
        if (roll < starChance + itemChance + eventChance + trapChance) return NodeType.Battle;
        return NodeType.Normal;
    }

    float GetTileSize(GameObject prefab)
    {
        if (prefab == null) return 1f;
        Renderer renderer = prefab.GetComponentInChildren<Renderer>();
        if (renderer != null) return renderer.bounds.size.x;
        return 1f;
    }

    TerrainType GetRegionType(int index)
    {
        int regionIndex = index / regionSize;
        return (TerrainType)(regionIndex % 3);
    }

    GameObject GetTilePrefab(TerrainType region)
    {
        return region switch
        {
            TerrainType.Forest => forestTilePrefab,
            TerrainType.Volcano => volcanoTilePrefab,
            TerrainType.Glacier => glacierTilePrefab,
            _ => nodePrefab
        };
    }
}
