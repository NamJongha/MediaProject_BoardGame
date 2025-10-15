using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Linq;
using static Unity.Collections.Unicode;

public class GameManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private int peerCount; // number of connected  clients(includes host) ; to check connection (debug)

    [SerializeField]
    private NetworkPrefabRef _playerPrefab;

    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new(); //players in room

    private NetworkRunner _runner;

    [SerializeField]
    private GameObject[] spawnPoints = new GameObject[4];

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        if (FindFirstObjectByType<NetworkRunner>() != null) return;
        StartCoroutine(StartAfterLoad());
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Scene loaded " + scene.name);

        if(scene.name == "TitleScene")
        {
            PlayerPrefs.SetString("currentScene", "TitleScene");
            PlayerPrefs.Save();
        }

        else if(scene.name == "LobbyScene")
        {
            PlayerPrefs.SetString("currentScene", "LobbyScene");
            PlayerPrefs.Save();
            spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint").OrderBy(spawnPoint => spawnPoint.name).ToArray();
            StartCoroutine(StartAfterLoad());
        }

        else if(scene.name == "GameScene")
        {
            PlayerPrefs.SetString("currentScene", "GameScene");
            PlayerPrefs.Save();
            spawnPoints = new GameObject[4];
            spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint").OrderBy(spawnPoint => spawnPoint.name).ToArray();
            StartCoroutine(StartAfterLoad());
        }
    }

    private IEnumerator StartAfterLoad()
    {
        yield return null;

        string curSceneName = PlayerPrefs.GetString("currentScene");
        Debug.Log("Current Scene: " + curSceneName);

        if (curSceneName == "LobbyScene")
        {
            string sessionName = PlayerPrefs.GetString("sessionName");
            string gameMode = PlayerPrefs.GetString("gameMode");

            Debug.Log("sesion: " + sessionName);

            GameStarter(gameMode, sessionName);
        }

        else if(curSceneName == "GameScene")
        {
            if (_runner.IsServer)
            {
                _spawnedCharacters.Clear();

                foreach (var player in _runner.ActivePlayers)
                {

                    Vector3 spawnPosition = new Vector3(0, 0, 0);
                    for (int i = 0; i < 4; i++)
                    {
                        if (spawnPoints[i].GetComponent<SpawnPointChecker>().getSpawned())//if player is already spawned on that point
                        {
                            continue; //look for next spawn point
                        }
                        else
                        {
                            spawnPosition = spawnPoints[i].transform.position + new Vector3(0, 2.5f, 0);
                            spawnPoints[i].GetComponent<SpawnPointChecker>().setSpawned();
                            break;
                        }
                    }
                    NetworkObject networkPlayerObject = _runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player); //매개변수 player: 아바타에 대한 입력 제공을 하는 플레이어

                    networkPlayerObject.GetComponent<Player>().resetReady(); //reset joined player's ready state to false

                    // Keep track of the player avatars for easy access
                    _spawnedCharacters.Add(player, networkPlayerObject);
                }
            }
        }
    }

    async void StartGame(GameMode mode, string sessionName)
    {
        // Create the Fusion runner and let it know that we will be providing user input
        _runner = gameObject.AddComponent<NetworkRunner>(); 
        _runner.ProvideInput = true;

        // Create the NetworkSceneInfo from the current scene
        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid)
        {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        }

        // Start or join (depends on gamemode) a session with a specific name
        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = sessionName,
            Scene = scene,
            PlayerCount = 4, //limiting player count
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        Debug.Log("Start session, mode: " + mode + " Session Name: " + sessionName);
    }

    public void GameStarter(string host, string sessionName)
    {
        if (_runner == null)//starting new session(if there's no session in current situation)
        {
            if (host == "host")
            {
                StartGame(GameMode.Host, sessionName);
            }
            else
            {
                StartGame(GameMode.Client, sessionName);
            }
        }
    }
    
    public Dictionary<PlayerRef, NetworkObject> GetPlayersList()
    {
        return _spawnedCharacters;
    }


    #region FusionMethods
    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("Connected to server");
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        throw new NotImplementedException();
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        throw new NotImplementedException();
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        throw new NotImplementedException();
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log("Host is missing");
        SceneManager.LoadScene("TitleScene");
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        throw new NotImplementedException();
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        if (Input.GetKey(KeyCode.W))
            data.direction += Vector3.forward;

        if (Input.GetKey(KeyCode.S))
            data.direction += Vector3.back;

        if (Input.GetKey(KeyCode.A))
            data.direction += Vector3.left;

        if (Input.GetKey(KeyCode.D))
            data.direction += Vector3.right;

        input.Set(data);
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        throw new NotImplementedException();
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer) //if the runner is host player
        {
            // Create a unique position for the player
            //Vector3 spawnPosition = new Vector3((player.RawEncoded % runner.Config.Simulation.PlayerCount) * 3, 1, 0);

            Vector3 spawnPosition = new Vector3(0, 0, 0);
            for (int i = 0; i < 4; i++)
            {
                if (spawnPoints[i].GetComponent<SpawnPointChecker>().getSpawned())//if player is already spawned on that point
                {
                    continue; //look for next spawn point
                }
                else
                {
                    spawnPosition = spawnPoints[i].transform.position + new Vector3(0, 2.5f, 0);
                    spawnPoints[i].GetComponent<SpawnPointChecker>().setSpawned();
                    break;
                }
            }
            NetworkObject networkPlayerObject = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player); //매개변수 player: 아바타에 대한 입력 제공을 하는 플레이어

            networkPlayerObject.GetComponent<Player>().resetReady(); //reset joined player's ready state to false

            // Keep track of the player avatars for easy access
            _spawnedCharacters.Add(player, networkPlayerObject);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            spawnPoints[player.PlayerId - 1].GetComponent<SpawnPointChecker>().deSpawned(); //player id starts from 1
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        throw new NotImplementedException();
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        throw new NotImplementedException();
    }

    //maybe we can use this methods to show loading scene
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        throw new NotImplementedException();
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log(shutdownReason);
        SceneManager.LoadScene("TitleScene");
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        throw new NotImplementedException();
    }

    #endregion

}
