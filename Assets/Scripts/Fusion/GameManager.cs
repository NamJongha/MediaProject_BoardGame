using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private int peerCount; // number of connected clients(includes host) ; to check connection (debug) 
    [SerializeField] private NetworkPrefabRef _playerPrefab;
    [SerializeField] private TurnManager turnManagerPrefab;

    [SerializeField] private GameObject[] spawnPoints = new GameObject[4];

    private readonly Dictionary<PlayerRef, NetworkObject>
        _spawnedCharacters =
            new(); //players in room

    private NetworkRunner _runner;
    private TurnManager _turnManager;

    private bool boardReady;
    private int boardSeed = -1;
    public static GameManager Instance { get; private set; }

    public NetworkPrefabRef PlayerPrefab => _playerPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        BoardGenerator.OnBoardGenerated += OnBoardReady;
    }

    private void Start()
    {
        if (FindFirstObjectByType<NetworkRunner>() != null) return;
        StartCoroutine(StartAfterLoad());
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("[Fusion] Scene Load Done: Respawn players");

        var curScene = PlayerPrefs.GetString("currentScene");

        // GAMETEST에서만 동작
        if (curScene == "GAMETEST")
        {
            // 1) 보드 생성 (Host에서 Seed 전달)
            if (runner.IsServer)
            {
                var seed = Random.Range(0, 999999);
                Debug.Log($"[Host] Generated Seed = {seed}");
                RPC_SendBoardSeed(seed);
            }

            // 2) 모든 플레이어 리스폰 코루틴 시작
            StartCoroutine(RespawnAllPlayersAfterBoardReady(runner));

            // 3) 🔥 TurnManager는 여기서 생성 (중복 방지)
            StartCoroutine(SpawnTurnManagerAfterRunnerReady());
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Scene loaded " + scene.name);
        if (scene.name == "TitleScene")
        {
            PlayerPrefs.SetString("currentScene", "TitleScene");
            PlayerPrefs.Save();
        }
        else if (scene.name == "LobbyScene")
        {
            PlayerPrefs.SetString("currentScene", "LobbyScene");
            PlayerPrefs.Save();
            spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint").OrderBy(spawnPoint => spawnPoint.name)
                .ToArray();
            StartCoroutine(StartAfterLoad());
        }
        else if (scene.name == "GAMETEST")
        {
            PlayerPrefs.SetString("currentScene", "GAMETEST");
            PlayerPrefs.Save();
            spawnPoints = new GameObject[4];
            spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint").OrderBy(spawnPoint => spawnPoint.name)
                .ToArray(); //spawn players
            StartCoroutine(StartAfterLoad());
        }
    }

    private IEnumerator StartAfterLoad()
    {
        yield return null;
        var curSceneName = PlayerPrefs.GetString("currentScene");
        Debug.Log("Current Scene: " + curSceneName);
        if (curSceneName == "LobbyScene")
        {
            var sessionName = PlayerPrefs.GetString("sessionName");
            var gameMode = PlayerPrefs.GetString("gameMode");
            Debug.Log("sesion: " + sessionName);
            GameStarter(gameMode, sessionName);
        }
        else if (curSceneName == "GAMETEST")
        {
            _runner.Spawn(turnManagerPrefab);
        }
    }

    private async void StartGame(GameMode mode, string sessionName)
    {
        // Create the Fusion runner and let it know that we will be providing user input
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput =
            true; // Create the NetworkSceneInfo from the current scene
        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid)
            sceneInfo.AddSceneRef(scene,
                LoadSceneMode.Additive); // Start or join (depends on gamemode) a session with a specific name await
        await _runner.StartGame(new StartGameArgs
        {
            GameMode = mode, SessionName = sessionName, Scene = scene, PlayerCount = 4, //limiting player count
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
        Debug.Log("Start session, mode: " + mode + " Session Name: " + sessionName);
    }

    public void GameStarter(string host, string sessionName)
    {
        if (_runner == null) //starting new session(if there's no session in current situation)
        {
            if (host == "host")
                StartGame(GameMode.Host, sessionName);
            else
                StartGame(GameMode.Client, sessionName);
        }
    }

    public Dictionary<PlayerRef, NetworkObject> GetPlayersList()
    {
        return _spawnedCharacters;
    }

    private IEnumerator RespawnAllPlayersAfterBoardReady(NetworkRunner runner)
    {
        // 보드 생성 완료까지 기다리기
        while (!boardReady)
            yield return null;

        // SpawnPoint 찾기
        spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint")
            .OrderBy(sp => sp.name)
            .ToArray();

        if (spawnPoints.Length == 0)
        {
            Debug.LogError("No SpawnPoints found in GAMETEST!");
            yield break;
        }

        Debug.Log($"[GM] Board ready, respawning players. SpawnPoints={spawnPoints.Length}");
        
        //내가 카페에서 이미 등록되어있는거에서 꺼내온다 했었는데, 내 로컬 코드 보니까 그냥 이걸 지우고 다시 씌우더라;;
        _spawnedCharacters.Clear();
        
        foreach (var player in runner.ActivePlayers)
        {
            var index = Mathf.Clamp(player.PlayerId - 1, 0, spawnPoints.Length - 1);
            var pos = spawnPoints[player.PlayerId - 1].transform.position;
            
            // 새로 스폰
            var newObj = _runner.Spawn(_playerPrefab, pos, Quaternion.identity, player);
            
            // 이부분 _spawnedCharacters[player] = newObj;로 되어있음
            // => 씬이 바뀔 때 기존 오브젝트가 디스폰되는데, Runner에 등록된 플레이어와 오브젝트 쌍은 변하지 않으면 문제 있을 수 있음
            runner.SetPlayerObject(player, newObj);
            _spawnedCharacters.Add(player, newObj);

            Debug.Log($"Respawned Player {player.PlayerId} at {pos}");
        }
    }

    private IEnumerator SpawnTurnManagerAfterRunnerReady()
    {
        // Runner 준비될 때까지 기다림
        while (_runner == null)
            yield return null;

        // Canvas 찾기
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[GM] Canvas not found! Cannot spawn TurnManager");
            yield break;
        }

        // 이미 존재한다면 삭제 (중복 방지)
        var oldTM = FindAnyObjectByType<TurnManager>();
        if (oldTM != null)
        {
            Destroy(oldTM.gameObject);
            Debug.Log("[GM] Old TurnManager destroyed");
        }

        // TurnManager 생성
        //var tm = Instantiate(turnManagerPrefab);
        //tm.transform.SetParent(GameObject.Find("TurnManagerRoot").transform);
        //Debug.Log("[GM] TurnManager spawned under Canvas.");
    }

    private void OnBoardReady()
    {
        boardReady = true;
        Debug.Log("[GM] Board ready.");

        //StartCoroutine(InitTurnManagerWhenPlayersReady());
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SendBoardSeed(int seed)
    {
        boardSeed = seed;
        Debug.Log($"[RPC] Seed Received: {seed}");

        // Seed를 받으면 즉시 보드 생성
        BoardGenerator.Instance.GenerateBoard(seed);
    }

/*
    private IEnumerator InitTurnManagerWhenPlayersReady()
    {
        // Runner 또는 플레이어 스폰 완료 대기
        while (_runner == null || _spawnedCharacters.Count != _runner.ActivePlayers.Count())
            yield return null;

        Debug.Log("[GM] All players spawned. Initializing TurnManager...");

        if (_turnManager == null)
        {
            Debug.LogError("[GM] ERROR: TurnManager is NULL!");
            yield break;
        }

        _turnManager.InitTurnFlow();
    }

    private IEnumerator SpawnPlayerWhenBoardReady(PlayerRef player)
    {
        while (!boardReady)
            yield return null;

        var bg = BoardGenerator.Instance;
        if (bg == null)
        {
            Debug.LogError("[GM] BoardGenerator.Instance 가 null 입니다!");
            yield break;
        }

        if (bg.spawnNodes == null || bg.spawnNodes.Count == 0)
        {
            Debug.LogError("[GM] SpawnNodes 가 비어 있습니다!");
            yield break;
        }

        var index = player.RawEncoded % bg.spawnNodes.Count;

        var spawnNode = bg.spawnNodes[index];

        var spawnPosition = spawnNode.transform.position + new Vector3(0, 2f, 0);

        var playerObj = _runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);

        _spawnedCharacters[player] = playerObj;
        Debug.Log($"[SPAWN DEBUG] Requested Spawn Pos = {spawnPosition}");
        Debug.Log($"Spawned player {player.PlayerId} at spawn node {spawnNode.id}");
    }

    private void SpawnPlayerInLobby(NetworkRunner runner, PlayerRef player)
    {
        var spawnPosition = Vector3.zero;

        for (var i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null)
                spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint")
                    .OrderBy(sp => sp.name).ToArray();

            var checker = spawnPoints[i].GetComponent<SpawnPointChecker>();
            if (!checker.getSpawned())
            {
                spawnPosition = spawnPoints[i].transform.position + Vector3.up * 2.5f;
                checker.setSpawned();
                break;
            }
        }

        var networkPlayerObject = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);
        networkPlayerObject.GetComponent<Player>().resetReady();

        _spawnedCharacters[player] = networkPlayerObject;

        Debug.Log($"Lobby Spawn: Player {player.PlayerId} at {spawnPosition}");
    }

    
    private void SpawnTurnManagerIfNeeded()
    {
        
        if (_turnManager != null)
            return;

        Debug.Log("[GM] Creating TurnManager AFTER Canvas exists");

        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogWarning("[GM] Canvas not found yet, delaying TurnManager creation...");
            StartCoroutine(DelaySpawnTurnManager());
            return;
        }

        var go = Instantiate(turnManagerPrefab, canvas.transform);
        _turnManager = go.GetComponent<TurnManager>();

        Debug.Log("[GM] TurnManager successfully created under Canvas");
        
    }

    private IEnumerator DelaySpawnTurnManager()
    {
        while (GameObject.Find("Canvas") == null)
            yield return null;

        SpawnTurnManagerIfNeeded();
    }*/


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
            Vector3 spawnPosition = new Vector3(0, 0, 0);
            for (int i = 0; i < 4; i++)
            {
                if (spawnPoints[i] == null){
                    spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint").OrderBy(spawnPoint => spawnPoint.name).ToArray();
                }
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
            NetworkObject networkPlayerObject = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.Euler(0, 0, 0), player); //�Ű����� player: �ƹ�Ÿ�� ���� �Է� ������ �ϴ� �÷��̾�

            networkPlayerObject.GetComponent<Player>().resetReady(); //reset joined player's ready state to false

            // Keep track of the player avatars for easy access
            _spawnedCharacters.Add(player, networkPlayerObject);
        }
        /*
        var curScene = PlayerPrefs.GetString("currentScene");

        // 🔵 로비에서는 즉시 스폰
        if (curScene == "LobbyScene")
        {
            SpawnPlayerInLobby(runner, player);
            return;
        }

        // 🔴 GAMETEST에서는 보드 생성 완료까지 대기
*/    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedCharacters.TryGetValue(player, out var networkObject))
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
    /*public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("Scene Load Done");
    }
*/
    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("Scene Load Start");
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        throw new NotImplementedException();
    }

    public async void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log(shutdownReason);

        await runner.Shutdown();

        if (runner != null) Destroy(runner.gameObject);

        SceneManager.LoadScene("TitleScene");
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        throw new NotImplementedException();
    }

    #endregion
}