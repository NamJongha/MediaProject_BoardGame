using System.Collections.Generic;
using UnityEngine;

public class BranchSelectorUI : MonoBehaviour
{
    public static BranchSelectorUI instance;
    public bool selectionDone = false;
    private BoardNode selected;

    void Awake() => instance = this;

    public BoardNode SelectBranch(List<BoardNode> branches)
    {
        selectionDone = false;
        Debug.Log("갈림길입니다. 방향을 선택하세요!");

        // (나중에 버튼 UI로 교체 가능)
        selected = branches[Random.Range(0, branches.Count)]; // 랜덤 선택
        selectionDone = true;
        return selected;
    }
}
