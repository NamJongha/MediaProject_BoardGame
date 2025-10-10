using UnityEngine;
using Fusion;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

public class TurnManager : MonoBehaviour
{
    [SerializeField] private Button turnDecideButton;

    private NetworkArray<PlayerRef> PlayerOrder { get; }

    private NetworkRunner _runner;
    private GameManager gameManager;

    private void Start()
    {
        turnDecideButton.onClick.AddListener(OnDecideButtonClicked);
        _runner = FindFirstObjectByType<NetworkRunner>();
        gameManager = FindFirstObjectByType<GameManager>();

        //need to be in scene loaded
        ResetOrder();
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

            result.Add((playerRef, dice));
        }

        result.Sort((player1, player2) => player2.dice.CompareTo(player1.dice));

        for(int i = 0; i < _runner.ActivePlayers.Count(); i++)
        {
            Debug.Log(i + " order player: " + result[i].player + " dice num: " + result[i].dice);
        }
    }

    private void ResetOrder()
    {
        PlayerOrder.Clear();
    }
}
