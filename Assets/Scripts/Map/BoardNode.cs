using UnityEngine;
using System.Collections.Generic;

public enum NodeType
{
    Normal,
    Branch,
    Special,
    Goal,
    Start
}

public enum TerrainType
{
    Forest,
    Desert,
    Volcano,
    Glacier
}

public class BoardNode : MonoBehaviour
{
    public int id;
    public NodeType nodeType;
    public TerrainType terrainType;
    public List<BoardNode> nextNodes = new();
    public bool isBranchNode = false; // 브랜치 노드 여부 추가

    void OnDrawGizmos()
    {
        switch (nodeType)
        {
            case NodeType.Start: Gizmos.color = Color.green; break;
            case NodeType.Goal: Gizmos.color = Color.red; break;
            case NodeType.Special: Gizmos.color = Color.blue; break;
            case NodeType.Branch: Gizmos.color = Color.yellow; break;
            default: Gizmos.color = Color.white; break;
        }

        Gizmos.DrawSphere(transform.position + Vector3.up * 0.5f, 0.3f);
    }
}
