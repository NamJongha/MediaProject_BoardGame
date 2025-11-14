using System.Collections.Generic;
using UnityEngine;

public enum NodeType
{
    Normal,
    Branch,
    Goal,
    Special
}

public class BoardNode : MonoBehaviour
{
    [Header("Node Info")]
    public int id;
    public TerrainType terrainType = TerrainType.Forest; // 노드가 속한 지형
    public NodeType nodeType = NodeType.Normal;          // 노드 타입 (기본, 갈림길, 골 등)
    public bool isBranchNode = false;                    // 갈림길 여부
    [SerializeField] private bool isSpawnNode = false;
    
    [Header("Connections")]
    public List<BoardNode> nextNodes = new List<BoardNode>(); // 다음으로 갈 수 있는 노드 목록

    [Header("Debug")]
    public Color debugColor = Color.white;

    public bool IsSpawnNode()
    {
        return isSpawnNode;
    }
    
    
    private void OnDrawGizmos()
    {
        Gizmos.color = debugColor;

        // 현재 노드 위치 표시
        Gizmos.DrawSphere(transform.position + Vector3.up * 0.3f, 0.3f);

        // 연결선 표시
        if (nextNodes != null)
        {
            foreach (var next in nextNodes)
            {
                if (next != null)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(transform.position + Vector3.up * 0.3f, next.transform.position + Vector3.up * 0.3f);
                }
            }
        }

        // 타입별 색상 표시
        switch (nodeType)
        {
            case NodeType.Goal:
                debugColor = Color.red;
                break;
            case NodeType.Branch:
                debugColor = Color.yellow;
                break;
            case NodeType.Special:
                debugColor = Color.magenta;
                break;
            default:
                debugColor = Color.white;
                break;
        }
    }
}