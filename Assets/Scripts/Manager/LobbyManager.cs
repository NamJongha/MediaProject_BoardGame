using UnityEngine;
using Fusion;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private Button gameStartButton;
    [SerializeField] private Button gameReadyButton;

    private Player localPlayer;
    private NetworkRunner runner;
    private GameManager gameManager;

    private bool isAllPlayerReady;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        runner = FindFirstObjectByType<NetworkRunner>();
        gameManager = FindFirstObjectByType<GameManager>();

        gameStartButton.onClick.AddListener(OnClickStartButton);
        gameReadyButton.onClick.AddListener(OnClickReadyButton);

        gameStartButton.gameObject.SetActive(false);
        gameReadyButton.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Lobby Manager is Running");

        if(runner == null)
        {
            runner = NetworkRunner.GetRunnerForScene(SceneManager.GetActiveScene());
            Debug.Log("LobbyManagerSet");
            //If the runner is null, repeat only the code above so that the cost gets smaller
            if (runner == null) return;
        }

        if (runner.IsServer)
        {
            Dictionary<PlayerRef, NetworkObject> playerList = gameManager.GetPlayersList(); //Dictionary from gameManager
            Debug.Log("There are " + playerList.Count + " players in the lobby");

            foreach(var kvp in playerList)
            {
                Debug.Log("Player " + kvp.Value.Id + " ready state: " + kvp.Value.GetComponent<Player>().isReady);
            }

            isAllPlayerReady = true;
            foreach(var keyValuePair in playerList)
            {
                NetworkObject playerObject = keyValuePair.Value;
                Player player = playerObject.GetComponent<Player>();

                if (player.isReady == false)
                {
                    isAllPlayerReady = false;
                    break;
                }
            }

            //gameStartButton.gameObject.SetActive(isAllPlayerReady);
        }
    }

    public void UpdateButtonState()
    {
        Debug.Log("update button called");

        if (runner == null)
        {
            runner = NetworkRunner.GetRunnerForScene(SceneManager.GetActiveScene());
            if(runner == null)
            {
                return;
            }
            Debug.Log("LobbyManagerSet");
        }

        if (runner.IsServer)
        {
            gameStartButton.gameObject.SetActive(true);
            gameReadyButton.gameObject.SetActive(false);
        }
        else
        {
            gameStartButton.gameObject.SetActive(false);
            gameReadyButton.gameObject.SetActive(true);
        }
    }

    //when host calls UpdateButtonState, runner is not allocated on LobbyManager, so allocate it manually when the Runner is intantiated
    public void SetRunner(NetworkRunner runner)
    {
        this.runner = runner;
    }

    public void SetLocalPlayer(Player player)
    {
        localPlayer = player;
    }

    private void OnClickReadyButton()
    {
        if (localPlayer == null) return;
        localPlayer.ChangeReady();
    }

    private void OnClickStartButton()
    {
        if (!runner.IsServer)
        {
            return;
        }
        
        if(isAllPlayerReady == false)
        {
            LogManager.Instance.Log("All players must be ready to start the game");
            return;
        }
        
        foreach(var kvp in gameManager.GetPlayersList())
        {
            if (kvp.Value != null)
            {
                runner.Despawn(kvp.Value);
                Debug.Log("player " + kvp.Key + " is despawned");
            }
        }
        runner.LoadScene("GameScene");
    }
}
