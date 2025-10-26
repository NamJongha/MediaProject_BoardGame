using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMover : MonoBehaviour
{
    public BoardNode currentNode;
    public float moveSpeed = 3f;
    private bool isMoving = false;

    public IEnumerator MoveSteps(int steps)
    {
        if (isMoving || currentNode == null) yield break;
        isMoving = true;

        for (int i = 0; i < steps; i++)
        {
            if (currentNode.nextNodes.Count == 0)
                break;

            BoardNode next = currentNode.nextNodes[0];

            // 갈림길일 경우
            if (currentNode.nextNodes.Count > 1)
            {
                next = BranchSelectorUI.instance.SelectBranch(currentNode.nextNodes);
                yield return new WaitUntil(() => BranchSelectorUI.instance.selectionDone);
            }

            yield return StartCoroutine(MoveToNode(next));
            currentNode = next;

            if (currentNode.nodeType == NodeType.Goal)
            {
                Debug.Log("도착 지점에 도달했습니다!");
                break;
            }
        }

        isMoving = false;
    }

    private IEnumerator MoveToNode(BoardNode next)
    {
        Vector3 start = transform.position;
        Vector3 end = next.transform.position;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }
    }
}
