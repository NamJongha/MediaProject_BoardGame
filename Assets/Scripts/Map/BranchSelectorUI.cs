using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Branch(갈림길) 선택 UI – 네트워크 완전 호환 버전
/// 구조:
/// 1) InputAuthority 플레이어에게만 UI 표시
/// 2) UI 버튼 클릭 → RPC_ChooseBranch(index) 호출 (Host로 전송)
/// 3) Host(StateAuthority)가 선택 확정
/// 4) PlayerMover가 Host가 선택한 경로로 이동
/// </summary>
public class BranchSelectorUI : MonoBehaviour
{
    public static BranchSelectorUI Instance;

    [Header("UI Elements")]
    [SerializeField] private GameObject panel;              // 전체 패널
    [SerializeField] private Button branchButtonPrefab;     // 버튼 프리팹
    [SerializeField] private Transform buttonContainer;     // 버튼이 들어갈 영역

    private Player currentPlayer;                          // 현재 턴 플레이어
    private List<BoardNode> currentBranches;               // 선택 가능한 분기 리스트

    private void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    /// <summary>
    /// 현재 턴 플레이어에게만 표시되는 UI
    /// </summary>
    public void Show(List<BoardNode> branches, Player player)
    {
        // UI는 반드시 InputAuthority(본인 턴 플레이어)에서만 보인다.
        if (!player.Object.HasInputAuthority)
            return;

        currentPlayer = player;
        currentBranches = branches;

        panel.SetActive(true);

        // 기존 버튼 제거
        foreach (Transform btn in buttonContainer)
            Destroy(btn.gameObject);

        // 버튼 생성
        for (int i = 0; i < branches.Count; i++)
        {
            int index = i;
            Button b = Instantiate(branchButtonPrefab, buttonContainer);

            // 버튼 텍스트 설정
            Text t = b.GetComponentInChildren<Text>();
            t.text = branches[i].name;

            // 클릭 시 → Host에게 선택 전달
            b.onClick.AddListener(() =>
            {
                OnBranchButtonClicked(index);
            });
        }
    }

    /// <summary>
    /// 클라이언트(InputAuthority)에서 버튼 클릭 시 실행됨
    /// </summary>
    private void OnBranchButtonClicked(int index)
    {
        if (currentPlayer == null)
            return;

        // Host에게 "이 갈림길(index) 선택함" RPC 요청
        currentPlayer.RPC_ChooseBranch(index);

        panel.SetActive(false);
    }

    /// <summary>
    /// 외부(PlayerMover)가 선택된 BoardNode 요청할 때 사용하는 함수
    /// Host에서 RPC_ChooseBranch(index) 완료 후 MoveSteps가 이어짐
    /// </summary>
    public BoardNode GetChosenNode(Player player)
    {
        if (!player.Object.HasStateAuthority)
        {
            Debug.LogError("[BranchSelectorUI] GetChosenNode는 반드시 StateAuthority(Host)에서 호출해야 합니다.");
            return null;
        }

        if (currentBranches == null || currentBranches.Count == 0)
            return null;

        int index = player.chosenBranchIndex;
        if (index < 0 || index >= currentBranches.Count)
        {
            Debug.LogError("[BranchSelectorUI] Branch 선택 인덱스가 범위를 벗어남!");
            return null;
        }

        return currentBranches[index];
    }
}

