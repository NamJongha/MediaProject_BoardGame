using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BranchSelectorUI : MonoBehaviour
{
    public static BranchSelectorUI Instance;

    [Header("UI Elements")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button branchButtonPrefab;

    private Dictionary<Player, List<BoardNode>> playerBranches = new Dictionary<Player, List<BoardNode>>();

    private void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    public void Show(List<BoardNode> branches, Player player)
    {
        if (!player.Object.HasInputAuthority)
            return;

        if (branches == null || branches.Count == 0)
        {
            Debug.LogError("[BranchSelectorUI] branches가 null이거나 비어 있음!");
            return;
        }

        playerBranches[player] = branches;

        panel.SetActive(true);

        // 기존 버튼 제거
        foreach (Transform child in panel.transform)
            Destroy(child.gameObject);

        // 버튼 생성 (panel이 직접 ButtonContainer 역할 수행)
        for (int i = 0; i < branches.Count; i++)
        {
            int index = i;

            Button btn = Instantiate(branchButtonPrefab, panel.transform);

            TMP_Text btnText = btn.GetComponentInChildren<TMP_Text>();
            btnText.text = (i == 0) ? "Main Path" : "Branch Path";

            btn.onClick.AddListener(() =>
            {
                OnBranchButtonClicked(player, index);
            });
        }
    }

    private void OnBranchButtonClicked(Player player, int index)
    {
        if (player == null)
            return;

        player.RPC_ChooseBranch(index);
        panel.SetActive(false);
    }

    public BoardNode GetChosenNode(Player player)
    {
        if (!player.Object.HasStateAuthority)
        {
            Debug.LogError("[BranchSelectorUI] GetChosenNode는 반드시 StateAuthority(Host)에서 호출해야 합니다.");
            return null;
        }

        if (!playerBranches.ContainsKey(player))
        {
            Debug.LogError("[BranchSelectorUI] 해당 플레이어에 대한 branch 목록이 없습니다!");
            return null;
        }

        List<BoardNode> branches = playerBranches[player];

        if (branches == null || branches.Count == 0)
        {
            Debug.LogError("[BranchSelectorUI] branches 리스트가 비어 있습니다.");
            return null;
        }

        int index = player.chosenBranchIndex;
        if (index < 0 || index >= branches.Count)
        {
            Debug.LogError("[BranchSelectorUI] Branch 선택 인덱스가 범위를 벗어남!");
            return null;
        }

        Debug.Log($"[BranchSelectorUI] Host에서 {player} → Branch {index} 선택 확정됨.");
        return branches[index];
    }
}
