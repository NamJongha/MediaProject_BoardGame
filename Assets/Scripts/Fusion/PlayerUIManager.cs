using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class PlayerUIManager : NetworkBehaviour
{
    private GameObject mainCanvas;
    private Player player;
    
    [SerializeField] private Button playerButtonPrefab;
    private Button endTurnButtonInstance;
    private Button diceRollButtonInstance;
    private Button useItemButtonInstance;
    private Button viewMapButtonInstance;
    private Button closeMapButtonInstance;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public override void Spawned()
    {
        base.Spawned();
        InstantiateButtons();
        
        mainCanvas = GameObject.Find("Canvas");
        player = this.gameObject.GetComponent<Player>();
    }

    public void InstantiateButtons()
    {
        #region instantiate buttons

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
            //endTurnButtonInstance.transform.SetParent(mainCanvas.transform);
            //diceRollButtonInstance.transform.SetParent(mainCanvas.transform);
            //useItemButtonInstance.transform.SetParent(mainCanvas.transform);
            //viewMapButtonInstance.transform.SetParent(mainCanvas.transform);
            //closeMapButtonInstance.transform.SetParent(mainCanvas.transform);
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
            endTurnButtonInstance.onClick.AddListener(OnEndTurnButtonClicked);
            closeMapButtonInstance.onClick.AddListener(OnClickCloseMap);

            //Initialize state
            SetButtonsActive(false);

            #endregion
    }
    
    public void SetButtonsActive(bool active)
    {
        endTurnButtonInstance.gameObject.SetActive(active);
        diceRollButtonInstance.gameObject.SetActive(active);
        useItemButtonInstance.gameObject.SetActive(active);
        viewMapButtonInstance.gameObject.SetActive(active);
        closeMapButtonInstance.gameObject.SetActive(active);
    }
    
    public void SetTurnButtonsVisible(bool visible)
    {
        Debug.Assert(endTurnButtonInstance != null);
        endTurnButtonInstance.gameObject.SetActive(visible);
        
        Debug.Assert(diceRollButtonInstance != null);
        diceRollButtonInstance.gameObject.SetActive(visible);
        
        Debug.Assert(useItemButtonInstance != null);
        useItemButtonInstance.gameObject.SetActive(visible);
        
        Debug.Assert(viewMapButtonInstance != null);
        viewMapButtonInstance.gameObject.SetActive(visible);
    }
    
    private void OnClickCloseMap()
    {
        if (player != null && player.Object.HasInputAuthority)
        {
            player.RequestControlMap();
        }
    }
    
    public void EnterMapViewUI()
    {
        // 턴 버튼들 숨기기
        SetTurnButtonsVisible(false);
        // 닫기 버튼만 보여주기
        closeMapButtonInstance.gameObject.SetActive(true);
    }

    public void ExitMapViewUI()
    {
        // 닫기 버튼 숨기기
        closeMapButtonInstance.gameObject.SetActive(false);
        // 턴 버튼 복귀
        SetTurnButtonsVisible(true);
    }
    #region ButtonEvents
    //End Turn Button
    private void OnEndTurnButtonClicked()
    {
        if (Object.HasInputAuthority)
        {
            if (player.isPlayerTurn)
            {
                RPC_RequestEndTurn();
            }
        }
    }
    
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestEndTurn()
    {
        Debug.Assert(TurnManager.Instance != null);

        TurnManager.Instance.OnPlayerEndTurn(Object.InputAuthority); //this is for managing turn state in turnManager
    }
    #endregion
}
