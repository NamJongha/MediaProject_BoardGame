using UnityEngine;
using Fusion;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private Button gameStartButton;
    [SerializeField] private Button gameReadyButton;

    private Player localPlayer;
    private NetworkRunner _runner;
    private GameManager gameManager;

    private bool isAllPlayerReady;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _runner = FindFirstObjectByType<NetworkRunner>();
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

        if(_runner == null)
        {
            _runner = NetworkRunner.GetRunnerForScene(SceneManager.GetActiveScene());
            Debug.Log("LobbyManagerSet");
            //If the runner is null, repeat only the code above so that the cost gets smaller
            if (_runner == null) return;
        }

        if (_runner.IsServer)
        {
            var playerList = gameManager.GetPlayersList(); //Dictionary from gameManager
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

        if (_runner == null)
        {
            _runner = NetworkRunner.GetRunnerForScene(SceneManager.GetActiveScene());
            if(_runner == null)
            {
                return;
            }
            Debug.Log("LobbyManagerSet");
        }

            if (_runner.IsServer)
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

    //호스트로 UpdateButtonState호출 시 LobbyManager에 Runner가 할당되지 않은 상태라 Runner 생성 시 수동으로 등록
    public void SetRunner(NetworkRunner runner)
    {
        _runner = runner;
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
        if (!_runner.IsServer) return;
        if(isAllPlayerReady == false)
        {
            Debug.Log("All the players should be logged in");
            return;
        }
        _runner.LoadScene("GameScene");
    }
}
