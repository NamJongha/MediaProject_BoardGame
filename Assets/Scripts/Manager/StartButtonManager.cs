using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartButtonManager : MonoBehaviour
{
    [SerializeField]
    private TMP_Text sessionNameText;
    private string sessionName;
    private string gameMode;

    [SerializeField]
    public GameObject FusionManager;

    public void SetSessionName()
    {
        this.sessionName = sessionNameText.text;
        Debug.Log(this.sessionName);
    }

    public void SetGameMode(string gameMode)
    {
        this.gameMode = gameMode;
    }
    
    public void ButtonClicked()
    {
        PlayerPrefs.SetString("sessionName", sessionName);
        PlayerPrefs.SetString("gameMode", gameMode);
        PlayerPrefs.Save();

        SceneManager.LoadScene("LobbyScene");

        //FusionManager.GetComponent<GameManager>().GameStarter(gameMode, sessionName);
    }
}
