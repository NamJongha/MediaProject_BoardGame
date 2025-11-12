using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class PlayerUIManager : NetworkBehaviour
{
    [SerializeField] private Button playerButtonPrefab;
    private Button endTurnButtonInstance;
    private Button diceRollButtonInstance;
    private Button useItemButtonInstance;
    private Button viewMapButtonInstance;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public override void Spawned()
    {
        base.Spawned();
        InstantiateButtons();
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

            //Set button's parent to canvas
            endTurnButtonInstance.transform.SetParent(GameObject.Find("Canvas").transform);
            diceRollButtonInstance.transform.SetParent(GameObject.Find("Canvas").transform);
            useItemButtonInstance.transform.SetParent(GameObject.Find("Canvas").transform);
            viewMapButtonInstance.transform.SetParent(GameObject.Find("Canvas").transform);

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

            //Set button's text
            endTurnButtonInstance.GetComponentInChildren<TMP_Text>().text = "End Turn";
            diceRollButtonInstance.GetComponentInChildren<TMP_Text>().text = "Roll Dice";
            useItemButtonInstance.GetComponentInChildren<TMP_Text>().text = "Use Item";
            viewMapButtonInstance.GetComponentInChildren<TMP_Text>().text = "View Map";

            //Add events on button
            endTurnButtonInstance.onClick.AddListener(OnEndTurnButtonClicked);

            //Initialize state
            SetButtonsActive(false);

            #endregion
    }

    private void OnEndTurnButtonClicked()
    {
        if (Object.HasInputAuthority)
        {
            if (this.gameObject.GetComponent<Player>().isPlayerTurn)
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
    
    public void SetButtonsActive(bool active)
    {
        endTurnButtonInstance.gameObject.SetActive(active);
        diceRollButtonInstance.gameObject.SetActive(active);
        useItemButtonInstance.gameObject.SetActive(active);
        viewMapButtonInstance.gameObject.SetActive(active);
    }
}
