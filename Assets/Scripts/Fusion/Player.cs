using System.Collections;
using Fusion;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Player : NetworkBehaviour
{
    private NetworkCharacterController _cc;
    private const int MaxStamina = 12;
    private const int MinStamina = 0;
    [Networked] public bool isReady { get; set; }
    [Networked] public bool isPlayerTurn { get; set; }
    [Networked] public NetworkString<_16> playerName { get; set; }
    [Networked] public int playerStamina { get; set; } = MaxStamina;
    [Networked] public int chosenBranchIndex { get; set; }
    [Networked] public NetworkBool branchSelected { get; set; }
    [Networked] private int diceBonus { get; set; } = 0;
    [Networked] public int currentNodeId { get; set; } = -1;

    [SerializeField] private GameObject nameObject; //text to show name above the player
    private TextMeshProUGUI nameText;

    private int diceNum = 0;
    
    private List<IItemStrategy> playerItemList = new List<IItemStrategy>(3);

    private ChangeDetector changeDetector;
    private TurnManager turnManager;

    
    [SerializeField] private Button playerButtonPrefab;
    private Button endTurnButtonInstance;
    private Button diceRollButtonInstance;
    private Button useItemButtonInstance;
    private Button viewMapButtonInstance;
    private Button closeMapButtonInstance;
    
    private PlayerUIManager playerUIManager;
    private ItemUIManager itemUIManager;

    private bool isMapMode = false;
    
    public bool isInitialized { get; private set; }
    
    private void Awake()
    
    {
        _cc = GetComponent<NetworkCharacterController>();
    }

    public override void Spawned()
    {
        Debug.Log("Player just spawned");

        changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        nameText = nameObject.GetComponent<TextMeshProUGUI>();

        if (Object.HasInputAuthority)
        {
            string _playerName = PlayerPrefs.GetString("playerName", $"Player_{Object.InputAuthority.PlayerId}"); //second variable is default value
            RPC_SetPlayerName(_playerName);
            turnManager = FindFirstObjectByType<TurnManager>();
            playerStamina = MaxStamina;
            LobbyManager lobby = FindFirstObjectByType<LobbyManager>();
            if (lobby != null)
            {
                Debug.Log("Found lobby manager");
                lobby.SetLocalPlayer(this);
                lobby.UpdateButtonState();
            }
            
            /*#region instantiate buttons

            endTurnButtonInstance = Instantiate(playerButtonPrefab);

            //instantiate player's menu button and rename it
            diceRollButtonInstance = Instantiate(playerButtonPrefab);
            diceRollButtonInstance.name = "DiceRollButton";
            useItemButtonInstance = Instantiate(playerButtonPrefab);
            useItemButtonInstance.name = "UseItemButton";
            viewMapButtonInstance = Instantiate(playerButtonPrefab);
            viewMapButtonInstance.name = "ViewMapButton";
            closeMapButtonInstance = Instantiate(playerButtonPrefab);
            closeMapButtonInstance.name = "CloseMapButton";

            //Set button's parent to canvas
            endTurnButtonInstance.transform.SetParent(GameObject.Find("Canvas").transform);
            diceRollButtonInstance.transform.SetParent(GameObject.Find("Canvas").transform);
            useItemButtonInstance.transform.SetParent(GameObject.Find("Canvas").transform);
            viewMapButtonInstance.transform.SetParent(GameObject.Find("Canvas").transform);
            closeMapButtonInstance.transform.SetParent(GameObject.Find("Canvas").transform);

            //Set button's position
            RectTransform rt;
            rt = endTurnButtonInstance.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(365, -250);
            rt = diceRollButtonInstance.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(365, 170);
            rt = useItemButtonInstance.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(365, 120);
            rt = viewMapButtonInstance.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(365, 70);
            rt = closeMapButtonInstance.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(800, 400);

            //Set button's text
            endTurnButtonInstance.GetComponentInChildren<TMP_Text>().text = "End Turn";
            diceRollButtonInstance.GetComponentInChildren<TMP_Text>().text = "Roll Dice";
            useItemButtonInstance.GetComponentInChildren<TMP_Text>().text = "Use Item";
            viewMapButtonInstance.GetComponentInChildren<TMP_Text>().text = "View Map";
            closeMapButtonInstance.GetComponentInChildren<TMP_Text>().text = "Close Map";

            //Add events on button
            //endTurnButtonInstance.onClick.AddListener(OnEndTurnButtonClicked);
            
            //Initialize state
            endTurnButtonInstance.gameObject.SetActive(false);
            diceRollButtonInstance.gameObject.SetActive(false);
            useItemButtonInstance.gameObject.SetActive(false);
            viewMapButtonInstance.gameObject.SetActive(false);
            closeMapButtonInstance.gameObject.SetActive(false);

            #endregion*/
            
            playerUIManager = gameObject.GetComponent<PlayerUIManager>();
            itemUIManager = gameObject.GetComponent<ItemUIManager>();
            string currentScene = SceneManager.GetActiveScene().name;
            SceneManager.activeSceneChanged += OnSceneChanged;
        }
        
        isInitialized = true;
    }

    public override void Render()
    {
        base.Render();

        if (nameText != null && !string.IsNullOrEmpty(playerName.ToString()))
        {
            if (nameText.text != playerName.ToString())
            {
                nameText.text = playerName.ToString();
            }

            foreach (var change in changeDetector.DetectChanges(this))
            {
                if (change == nameof(isPlayerTurn))
                {
                    Debug.Log("isPlayerTurnChanged");
                    if (Object.HasInputAuthority)
                    {
                        //show button only when the player turn comes
                        playerUIManager.SetTurnButtonsVisible(isPlayerTurn);
                    }
                }

                if (change == nameof(playerName))
                {
                    Debug.Assert(nameText != null);
                    nameText.text = playerName.ToString();
                    if (Object.HasStateAuthority)
                    {
                        LogManager.Instance.Log($"{playerName} joined the session");
                    }
                }
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            if (isPlayerTurn)
            {
                data.direction.Normalize();
                _cc.Move(5 * data.direction * Runner.DeltaTime);
            }
            else
            {
                Vector3 gravity = new Vector3(0, -1, 0);
                _cc.Move(5 * gravity * Runner.DeltaTime);

                transform.up = Vector3.up;
                transform.rotation = Quaternion.Euler(0, 180, 0);
            }
        }

        Vector3 euler = gameObject.transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(0, euler.y, 0);
    }

    //Player Name Setting
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetPlayerName(string name)
    {
        playerName = name;
    }

    #region Button Events

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestEndTurn()
    {
        if (turnManager == null)
        {
            turnManager = FindFirstObjectByType<TurnManager>();
        }

        turnManager.OnPlayerEndTurn(Object.InputAuthority); //this is for managing turn state in turnManager
    }
    
    private void OnSceneChanged(Scene prev, Scene next)
    {
        if (Object.HasInputAuthority && next.name == "GAMETEST")
        {
            StartCoroutine(InitAfterSceneCoroutine());
        }
    }
    
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_InitAfterSceneLoad()
    {
        Debug.Log($"[Player] InitAfterSceneLoad for {playerName}");

        if (Object.HasInputAuthority)
        {
            itemUIManager = FindFirstObjectByType<ItemUIManager>();
            playerUIManager = FindAnyObjectByType<PlayerUIManager>();
        }
    }
    
    private IEnumerator InitAfterSceneCoroutine()
    {
        // UI가 1프레임 뒤에 생성되므로 딜레이 필요
        yield return null;
        yield return new WaitForSeconds(0.1f);

        itemUIManager = FindFirstObjectByType<ItemUIManager>();
        playerUIManager = FindAnyObjectByType<PlayerUIManager>();

        if (itemUIManager == null)
            Debug.LogError("[Player] ItemUIManager 여전히 null!");
        if (playerUIManager == null)
            Debug.LogError("[Player] PlayerUIManager 여전히 null!");
    }
    
    public void OnDiceRollButtonClicked()
    {
        if (Object.HasInputAuthority)
        {
            if (isPlayerTurn)
            {
                playerUIManager.SetTurnButtonsVisible(false);
                RPC_RequestRollDice();
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestRollDice()
    {
        if (turnManager == null)
        {
            turnManager = FindFirstObjectByType<TurnManager>();
        }
        int staminaLossCount = (MaxStamina - playerStamina) / 2;
        int maxDiceValue = Mathf.Max(1, 6 - staminaLossCount);

        int diceValue = UnityEngine.Random.Range(1, maxDiceValue + 1);

        LogManager.Instance.Log(
            $"{playerName} rolled {diceValue} (max: {maxDiceValue}, stamina: {playerStamina})"
        );
        
        // Start moving the player
        PlayerMover mover = GetComponent<PlayerMover>();

        // MoveStepsAndFinishTurn(steps, player, turnManager)
        mover.StartCoroutine(mover.MoveStepsAndFinishTurn(diceValue, this, turnManager));
        
        // Stamina decreases by 1 each turn
        playerStamina = Mathf.Max(0, playerStamina - 1);

        // Update Stamina Text and UI
        LogManager.Instance.Log($"{playerName} stamina is now {playerStamina}");
    }
    
    //public void RequestOpenItemUI()
    //{
    //    if (!Object.HasInputAuthority) return;
    //    if (!isPlayerTurn) return;
    //    playerUIManager.SetTurnButtonsVisible(false);
    //    RPC_OpenItemUI(Object.InputAuthority);
    //}
    //[Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    //private void RPC_OpenItemUI(PlayerRef owner)
    //{
    //    if (itemUIManager == null)
    //        itemUIManager = FindFirstObjectByType<ItemUIManager>();
//
    //    // UI 노출 (모든 클라이언트)
    //    itemUIManager.ShowItemList(this, owner);
    //}
    
    public void RequestUseItem(int index)
    {
        if (!Object.HasInputAuthority) return;
        if (!isPlayerTurn) return;
//
        RPC_RequestUseItem(index);
    }
//
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestUseItem(int index)
    {
        var list = GetItemList();
        if (index < 0 || index >= list.Count)
        {
            return;
        }
//
        var item = list[index];
//
        // 아이템 효과 실행
        item.UseItem(this);
//
        // 인벤토리 삭제
        RemoveItem(item);
//
        // UI 닫기
        RequestCloseItemUI();
    }
    
    public void RequestCloseItemUI()
    {
        if (Object.HasInputAuthority)
        {
            RPC_RequestCloseItemUI();   // 🆕 클라이언트 → 서버 요청
            playerUIManager.SetTurnButtonsVisible(true);
        }
    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestCloseItemUI()
    {
        RPC_CloseItemUI();  // 🆕 서버가 실행 → 권한 OK
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_CloseItemUI()
    {
        if (itemUIManager == null)
        {
            itemUIManager = FindAnyObjectByType<ItemUIManager>();
            if (itemUIManager == null)
            {
                Debug.LogError("[Player] itemUIManager is STILL NULL in RPC_CloseItemUI()!");
                return;
            }
        }
        itemUIManager.CloseUI();
        
        if (Object.HasInputAuthority)
        {
            playerUIManager.SetTurnButtonsVisible(true);
        }
    }
    
    public void OnMapControlButtonClicked()
    {
        if (Object.HasInputAuthority)
        {
            if (isPlayerTurn)
            {
                RPC_RequestControlViewMap();
            }
            CameraManager.Instance.SetIsLocalPlayerTurn(isPlayerTurn);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestControlViewMap()
    {
        if (turnManager == null)
        {
            turnManager = FindFirstObjectByType<TurnManager>();
        }
        
        bool enteringMapMode = !isMapMode;
        
        isMapMode = enteringMapMode;

        // RPC로 전체 클라이언트에게 반영
        RPC_ApplyViewMapModeLocal(enteringMapMode);

    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ApplyViewMapModeLocal(bool entering)
    {
        if (entering)
        {
            CameraManager.Instance.EnterViewMap();

            if (Object.HasInputAuthority)
            {
                playerUIManager.EnterMapViewUI();
            }
        }
        else
        {
            CameraManager.Instance.ExitViewMap();
            CameraManager.Instance.SetPlayerCamera(this.Object);

            if (Object.HasInputAuthority)
            {
                playerUIManager.ExitMapViewUI();
            }
        }
    }

    #endregion

    #region player ready in lobby

    public void ChangeReady()
    {
        if (Object.HasStateAuthority)
        {
            isReady = !isReady;
        }
        else
        {
            RPC_RequestChangeReady(!isReady);
        }

        string msg = isReady ? "not ready" : "ready";
        LogManager.Instance.Log($"{playerName} {msg}");
    }

    //isReady should be changed by the Host -> client send change to host with rpc
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestChangeReady(bool state)
    {
        isReady = state;
    }

    public void resetReady()
    {
        if (Object.HasStateAuthority)
        {
            if (Object.InputAuthority == Runner.LocalPlayer) isReady = true;
            else isReady = false;
        }
    }

    #endregion

    #region player turn state

    public void ChangeIsPlayerTurn(bool state)
    {
        if ((Runner.IsServer))
        {
            isPlayerTurn = state;
        }
        else
        {
            RPC_RequestChangeIsPlayerTurn(state);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestChangeIsPlayerTurn(bool state)
    {
        isPlayerTurn = state;
    }

    public void ResetIsPlayerTurn()
    {
        isPlayerTurn = false;
    }

    #endregion

    #region player dice logic

    public void RollTheDice()
    {
        int dice = Random.Range(1, 7);
        diceNum = dice;
    }

    public int GetDiceNum()
    {
        return diceNum;
    }

    public void SetDiceNum(int diceNum)
    {
        this.diceNum = diceNum;
    }

    #endregion

    public List<IItemStrategy> GetItemList()
    {
        return playerItemList;
    }
    
    public void ModifyStamina(int amount)
    {
        playerStamina = Mathf.Clamp(playerStamina + amount, MinStamina, MaxStamina);
    }
    
    public bool AddItem(IItemStrategy item)
    {
        if (item == null)
        {
            Debug.LogError("[Player] Null 아이템은 추가할 수 없습니다.");
            return false;
        }

        if (playerItemList.Count >= 3)
        {
            Debug.Log($"[Player] {playerName} 아이템 슬롯이 가득 찼습니다.");
            LogManager.Instance.Log($"[Player] {playerName} inventory is full!");
            return false;
        }

        playerItemList.Add(item);

        LogManager.Instance.Log($"[Player] {playerName} got Item: {item.GetName()}");
/*
        // 아이템 UI 업데이트 (존재할 경우)
        if (ItemUIManager.Instance != null)
            ItemUIManager.Instance.RefreshUI(this);
*/
        return true;
    }

    public bool RemoveItem(IItemStrategy item)
    {
        if (item == null)
        {
            Debug.LogError("[Player] RemoveItem()에 null이 전달됨");
            return false;
        }

        if (!playerItemList.Contains(item))
        {
            Debug.LogWarning($"[Player] {playerName} 아이템 리스트에 없음: {item.GetName()}");
            return false;
        }

        playerItemList.Remove(item);

        Debug.Log($"[Player] {playerName} used item: {item.GetName()}");
        LogManager.Instance.Log($"[Player] {playerName} used item: {item.GetName()}");
/*
        // 사용 후 UI 업데이트
        if (ItemUIManager.Instance != null)
            ItemUIManager.Instance.RefreshUI(this);
*/
        return true;
    }

    public void AddDiceBonus(int bonus)
    {
        diceBonus += bonus;
        LogManager.Instance.Log($"{playerName} received dice bonus: +{bonus}");
    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ChooseBranch(int index)
    {
        chosenBranchIndex = index;
        branchSelected = true;
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_OpenBranchSelector(int[] branchNodeIds)
    {
        // BoardNode id 리스트를 실제 노드 참조 리스트로 복원
        var branches = new List<BoardNode>();
        foreach (var id in branchNodeIds)
        {
            var node = BoardGenerator.Instance.GetNodeById(id);
            if (node != null)
                branches.Add(node);
        }

        if (branches.Count == 0)
        {
            Debug.LogError("[Player] RPC_OpenBranchSelector: no valid branches found.");
            return;
        }

        if (BranchSelectorUI.Instance != null)
        {
            BranchSelectorUI.Instance.Show(branches, this);
        }
        else
        {
            Debug.LogError("[Player] RPC_OpenBranchSelector: BranchSelectorUI.Instance is null.");
        }
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_OnGameOver()
    {
        if (playerUIManager != null)
        {
            playerUIManager.SetTurnButtonsVisible(false);
        }

        isPlayerTurn = false;
    }
    
}