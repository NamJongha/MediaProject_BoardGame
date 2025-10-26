using System.Collections.Generic;
using UnityEngine;

public enum NodeType { Normal, Item, Event, Battle, Star, Goal }
public enum TerrainType { Forest, Volcano, Glacier }

public class BoardNode : MonoBehaviour
{
    public int id;
    public NodeType nodeType = NodeType.Normal;
    public TerrainType terrainType = TerrainType.Forest;
    public List<BoardNode> nextNodes = new List<BoardNode>();

    private void OnDrawGizmos()
    {
        // 지형색 + 이벤트색 혼합 표시
        Color baseColor = terrainType switch
        {
            TerrainType.Forest => new Color(0.3f, 0.8f, 0.3f),
            TerrainType.Volcano => new Color(0.9f, 0.3f, 0.1f),
            TerrainType.Glacier => new Color(0.5f, 0.8f, 1f),
            _ => Color.white
        };

        Color eventColor = nodeType switch
        {
            NodeType.Goal => Color.yellow,
            NodeType.Item => Color.green,
            NodeType.Event => Color.cyan,
            NodeType.Battle => Color.red,
            NodeType.Star => Color.magenta,
            _ => baseColor
        };

        Gizmos.color = eventColor;
        Gizmos.DrawSphere(transform.position, 0.35f);

        Gizmos.color = Color.white;
        foreach (var next in nextNodes)
        {
            if (next != null)
                Gizmos.DrawLine(transform.position, next.transform.position);
        }
    }
}
