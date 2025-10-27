using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using WebSocketSharp;

public class StartButtonManager : MonoBehaviour
{
    [SerializeField]
    private TMP_Text sessionNameText;
    [SerializeField]
    private TMP_Text playerNameText;
    private string sessionName;
    private string playerName;
    private string gameMode;

    [SerializeField]
    public GameObject FusionManager;

    public void SetSessionName()
    {
        this.sessionName = sessionNameText.text;
        Debug.Log(this.sessionName);
    }

    public void SetPlayerName()
    {
        this.playerName = playerNameText.text;
        Debug.Log(this.playerName);
    }

    public void SetGameMode(string gameMode)
    {
        this.gameMode = gameMode;
    }
    
    public void ButtonClicked()
    {
        if(sessionName == null || string.IsNullOrWhiteSpace(sessionName))
        {
            Debug.Log("Session Name should have at least one alphabat");
            return;
        }

        PlayerPrefs.SetString("sessionName", sessionName);
        PlayerPrefs.SetString("playerName", playerName);
        PlayerPrefs.SetString("gameMode", gameMode);
        PlayerPrefs.Save();

        SceneManager.LoadScene("LobbyScene");

        //FusionManager.GetComponent<GameManager>().GameStarter(gameMode, sessionName);
    }
}
