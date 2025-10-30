using UnityEngine;
using Fusion;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class TurnManager : NetworkBehaviour
{
    private Button turnDecideButton;
    private Button turnStartButton;

    [Networked, Capacity(4)] public NetworkArray<PlayerRef> PlayerOrder => default;
    [Networked] public TurnState curState { get; set; } = TurnState.WaitingForOrder;
    [Networked] public int curTurnIndex { get; set; } = -1;

    private GameManager gameManager;

    private void SetState(TurnState state)
    {
        if (!Object.HasStateAuthority) return;

        if(turnDecideButton == null || turnStartButton == null)
        {
            return;
        }

        Debug.Log("Change current state to "  + state);
        curState = state;

        switch (state)
        {
            case TurnState.WaitingForOrder:
                turnDecideButton.gameObject.SetActive(true);
                turnStartButton.gameObject.SetActive(false);
                break;

            case TurnState.DecidingOrder:
                turnDecideButton.gameObject.SetActive(false);
                turnStartButton.gameObject.SetActive(true);
                break;

            case TurnState.TurnStart:
                break;

            case TurnState.TurnAction:
                turnStartButton.gameObject.SetActive(false);
                break;

            case TurnState.TurnEnd:
                EndTurn();
                break;

            case TurnState.GameOver:
                break;
        }
    }

    private void Awake()
    {
        Button[] buttons = new Button[2];
        buttons = FindObjectsByType<Button>(sortMode: default);

        foreach(var btn in buttons)
        {
            if (btn.name == "OrderDecideButton") turnDecideButton = btn;
            else if (btn.name == "TurnStartButton") turnStartButton = btn;
        }
    }

    public override void Spawned()
    {
        base.Spawned();

        turnDecideButton.onClick.AddListener(OnDecideButtonClicked);
        turnStartButton.onClick.AddListener(OnTurnStartButtonClicked);

        gameManager = FindFirstObjectByType<GameManager>();

        ResetOrder();

        if (Object.HasStateAuthority)
        {
            StartCoroutine(DelayedInitState());
        }
        else
        {
            turnDecideButton.gameObject.SetActive(false);
            turnStartButton.gameObject.SetActive(false);
        }
    }

    private IEnumerator DelayedInitState()
    {
        yield return null;
        yield return null;

        Debug.Log("TurnManager: DelayedInitState called, setting to WaitingForOrder");
        SetState(TurnState.WaitingForOrder);
    }

    private void OnDecideButtonClicked()
    {
        if (Object.HasStateAuthority)
        {

            if (curState == TurnState.WaitingForOrder)
            {
                SetState(TurnState.DecidingOrder);
            }

            DecideTurnOrder();
        }
    }

    private void OnTurnStartButtonClicked()
    {
        if (Object.HasStateAuthority) {
        StartTurn();
            }
    }

    private void DecideTurnOrder()
    {
        //each player roll the dice
        //after all the players roll it, order it from big num to small num
        //big number start first

        List<(PlayerRef player, int dice)> result = new List<(PlayerRef, int)>();

        foreach(var kvp in gameManager.GetPlayersList())
        {
            kvp.Value.GetComponent<Player>().RollTheDice();

            PlayerRef playerRef = kvp.Key;
            int dice = kvp.Value.GetComponent<Player>().GetDiceNum();

            Debug.Log(playerRef + " player's dice number: " + dice);
            Debug.Log("This Player's name is " + kvp.Value.gameObject.name);
            result.Add((playerRef, dice));
        }

        result.Sort((player1, player2) => player2.dice.CompareTo(player1.dice));

        for(int i = 0; i < Runner.ActivePlayers.Count(); i++)
        {
            PlayerOrder.Set(i, result[i].player);
            Debug.Log(i + " order player: " + PlayerOrder[i]);
        }
    }

    private void StartTurn()
    {
        if (!Object.HasStateAuthority) return;

        if(curTurnIndex == -1) curTurnIndex = 0;
        else
        {
            curTurnIndex = (curTurnIndex + 1) % Runner.ActivePlayers.Count();
        }

        PlayerRef curPlayerRef = PlayerOrder.Get(curTurnIndex);
        Debug.Log("Cur Turn index: " + curTurnIndex);

        NetworkObject curPlayerObj = Runner.GetPlayerObject(curPlayerRef);
        Debug.Log("Cur Player Obj: + " + curPlayerObj.gameObject.name);

        curPlayerObj.GetComponent<Player>().ChangeIsPlayerTurn(true);
        Debug.Log(curPlayerRef + " turn started");

        SetState(TurnState.TurnAction);
    }

    public void OnPlayerEndTurn(PlayerRef player)
    {
        if (Object.HasStateAuthority && PlayerOrder.Get(curTurnIndex) ==  player)
        {
            SetState(TurnState.TurnEnd);
        }
    }

    private void EndTurn()
    {
        PlayerRef curPlayerRef = PlayerOrder.Get(curTurnIndex);
        NetworkObject curPlayerObj = Runner.GetPlayerObject(curPlayerRef);

        curPlayerObj.GetComponent<Player>().ChangeIsPlayerTurn(false);

        if (Object.HasStateAuthority)
        {
            StartCoroutine(DelayedStartTurn(1.0f));
        }
    }

    private IEnumerator DelayedStartTurn(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (Object.HasStateAuthority)
        {
            foreach (var kvp in gameManager.GetPlayersList())
            {
                kvp.Value.GetComponent<Player>().ChangeIsPlayerTurn(false);
            }

            yield return null; // wait for one frame to load it

            SetState(TurnState.TurnStart);
            StartTurn(); // restart the turn after end (next player)
        }
    }


    public void ResetOrder()
    {
        PlayerOrder.Clear();
    }
}

#region enum for turn state
public enum TurnState //Finite State Machine
{
    WaitingForOrder,
    DecidingOrder,
    TurnStart,
    TurnAction,
    TurnEnd,
    GameOver
}
#endregion
