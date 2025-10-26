using UnityEngine;
using Fusion;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

public class TurnManager : NetworkBehaviour
{
    [SerializeField] private Button turnDecideButton;

    [Networked, Capacity(4)] public NetworkArray<PlayerRef> PlayerOrder => default;

    private NetworkRunner _runner;
    private GameManager gameManager;

    private void Start()
    {

    }

    public override void Spawned()
    {
        base.Spawned();

        turnDecideButton = FindFirstObjectByType<Button>();
        turnDecideButton.onClick.AddListener(OnDecideButtonClicked);
        _runner = FindFirstObjectByType<NetworkRunner>();
        gameManager = FindFirstObjectByType<GameManager>();
    }

    private void OnDecideButtonClicked()
    {
        DecideTurnOrder();
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
            result.Add((playerRef, dice));
        }

        result.Sort((player1, player2) => player2.dice.CompareTo(player1.dice));

        for(int i = 0; i < _runner.ActivePlayers.Count(); i++)
        {
            PlayerOrder.Set(i, result[i].player);
            Debug.Log(i + " order player: " + PlayerOrder[i]);
        }
    }

    public void ResetOrder()
    {
        PlayerOrder.Clear();
    }
}
