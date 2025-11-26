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
}
