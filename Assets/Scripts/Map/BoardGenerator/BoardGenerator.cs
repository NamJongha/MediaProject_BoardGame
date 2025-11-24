using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public enum TerrainType
{
    Default,
    Forest,
    Desert,
    Volcano,
    Glacier
}

[Serializable]
public class TerrainRegion
{
    public TerrainType terrainType;
    public RectInt area;
    public int specialTileCount;
    public GameObject tilePrefab;
}

public class BoardGenerator : MonoBehaviour
{
    [Header("References")] public PathGenerator pathGenerator;

    public SpecialTileGenerator specialTileGenerator;
    public Transform tileParent;

    [Header("Node Prefabs")] public GameObject defaultNodePrefab;
    
    [Header("Path Settings")] [Range(5, 1000)]
    public int mainPathLength = 60;

    public Vector2Int startCoord = new(0, 0);
    public Vector2Int endCoord = new(200, 0);
    [Range(0.5f, 2f)] public float spacingMultiplier = 1.1f;

    [Header("Branch Settings")] [Range(0f, 1f)]
    public float branchChance = 0.2f;

    public float branchInterval = 10f;

    [Header("Terrain Regions")] public List<TerrainRegion> terrainRegions = new();

    private List<BoardNode> allNodes = new List<BoardNode>();
    public List<BoardNode> spawnNodes = new List<BoardNode>();
    private BoardNode goalNode;
    
    public static BoardGenerator Instance { get; private set; }
    private void Start()
    {
    }
    
    private void Awake()
    {
        Instance = this;
    }

    public static event Action OnBoardGenerated;
    
    private void CollectSpawnNodes()
    {
        spawnNodes = FindObjectsOfType<BoardNode>()
            .Where(n => n.IsSpawnNode())
            .OrderBy(n => n.transform.name)
            .ToList();

        Debug.Log($"[BoardGenerator] SpawnNodes collected = {spawnNodes.Count}");

    }
    public void GenerateBoard(int seed)
    {
        Debug.Log($"[BoardGenerator] GenerateBoard(seed={seed})");

        UnityEngine.Random.InitState(seed);

        ClearBoard();
        allNodes.Clear();

        var tileSize = GetTileSize(defaultNodePrefab);

        // 메인 경로 + 브랜치 생성
        pathGenerator.GenerateMainPath(this, allNodes, tileSize);

        StartCoroutine(pathGenerator.GenerateBranches(this, allNodes, tileSize));
                
        // Goal 노드 추가
        CreateGoalNode();
        
        // 브랜치 생성 코루틴이 끝나도록 약간 기다렸다가 특수 타일 생성
        StartCoroutine(WaitAndGenerateSpecialTiles());
        
        CollectSpawnNodes();
        OnBoardGenerated?.Invoke();
    }

    private void CreateGoalNode()
    {
        var goalPos = new Vector3(endCoord.x, 0, endCoord.y);

        // 1️⃣ 기존 Goal 근처 중복 노드 제거
        allNodes.RemoveAll(n => n == null);
        for (var i = allNodes.Count - 1; i >= 0; i--)
            if (Vector3.Distance(allNodes[i].transform.position, goalPos) < 0.5f)
            {
                DestroyImmediate(allNodes[i].gameObject);
                allNodes.RemoveAt(i);
            }

        // 2️⃣ Goal 노드 생성
        var goalObj = Instantiate(defaultNodePrefab, goalPos, Quaternion.identity, tileParent);
        goalNode = goalObj.GetComponent<BoardNode>() ?? goalObj.AddComponent<BoardNode>();
        goalNode.id = allNodes.Count;
        goalNode.nodeType = NodeType.Goal;
        goalNode.terrainType = GetRegionByPosition(goalPos);

        // 색상 지정
        var renderer = goalObj.GetComponentInChildren<Renderer>();
        if (renderer != null) renderer.material.color = Color.red;

        // 3️⃣ 가장 가까운 노드 찾기 (Goal 연결용)
        BoardNode closest = null;
        var closestDist = float.MaxValue;
        foreach (var n in allNodes)
        {
            if (n == null) continue;
            var d = Vector3.Distance(n.transform.position, goalPos);
            if (d < closestDist)
            {
                closestDist = d;
                closest = n;
            }
        }

        // 4️⃣ Goal 연결
        if (closest != null)
            // Goal이 마지막에 정확히 연결되도록 방향 보정
            if (!closest.nextNodes.Contains(goalNode))
                closest.nextNodes.Add(goalNode);

        // 5️⃣ 마지막 간격이 타일 간격보다 크면 중간 보정 노드 생성
        if (closest != null && closestDist > GetTileSize(defaultNodePrefab) * 2f)
        {
            var midPos = Vector3.Lerp(closest.transform.position, goalPos, 0.5f);
            var midObj = Instantiate(defaultNodePrefab, midPos, Quaternion.identity, tileParent);
            var midNode = midObj.GetComponent<BoardNode>() ?? midObj.AddComponent<BoardNode>();
            midNode.id = allNodes.Count;
            midNode.terrainType = GetRegionByPosition(midPos);
            midNode.nodeType = NodeType.Normal;

            closest.nextNodes.Clear();
            closest.nextNodes.Add(midNode);
            midNode.nextNodes.Add(goalNode);

            allNodes.Add(midNode);
            Debug.Log($"Goal과 거리가 {closestDist:F2} → 중간 노드 추가로 보정 완료");
        }

        // 6️⃣ Goal을 전체 노드 목록에 추가
        allNodes.Add(goalNode);
    }


    private IEnumerator WaitAndGenerateSpecialTiles()
    {
        yield return new WaitForSeconds(0.5f);
        
        specialTileGenerator.GenerateSpecialTiles(this);

        //OnBoardGenerated?.Invoke();
        Debug.Log("맵 생성 완료, 노드 수: " + allNodes.Count);
    }

    public void RegenerateWithRuntimeSettings()
    {
        //GenerateBoard(seed);
    }

    private void ClearBoard()
    {
        if (tileParent == null) return;

        for (var i = tileParent.childCount - 1; i >= 0; i--) DestroyImmediate(tileParent.GetChild(i).gameObject);
    }

    private float GetTileSize(GameObject prefab)
    {
        if (prefab == null) return 1f;
        var rend = prefab.GetComponentInChildren<Renderer>();
        if (rend == null) return 1f;
        return rend.bounds.size.x;
    }

    // 이 위치가 어떤 지형인지 판정
    public TerrainType GetRegionByPosition(Vector3 pos)
    {
        Vector2Int p = new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.z));

        foreach (var region in terrainRegions)
        {
            // RectInt는 (x, y, width, height) 구조인데, y는 z축에 해당함
            if (region.area.Contains(p))
                return region.terrainType;
        }

        return TerrainType.Forest; // 기본값
    }

    // 지형 타입에 맞는 타일 프리팹 반환
    public GameObject GetTilePrefab(TerrainType type)
    {
        foreach (var region in terrainRegions)
            if (region.terrainType == type && region.tilePrefab != null)
                return region.tilePrefab;
        return defaultNodePrefab;
    }

    public List<BoardNode> GetAllNodes()
    {
        return allNodes; // List는 참조 타입이므로 그대로 공유됨
    }
    
    public BoardNode GetStartNode()
    {
        if (spawnNodes == null || spawnNodes.Count == 0)
        {
            Debug.LogError("Start nodes not set!");
            return null;
        }

        return allNodes[0];
    }
    
    public BoardNode GetNodeById(int id)
    {
        if (id < 0 || id >= allNodes.Count)
            return null;

        return allNodes[id];
    }

}