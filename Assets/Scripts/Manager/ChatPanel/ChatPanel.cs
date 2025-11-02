using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatPanel : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject debugLogPrefab;

    private void Start()
    {
        Debug.Assert(LogManager.Instance != null);
        LogManager.Instance.OnRecievedLog += AddLog;
    }

    public void AddLog(string log)
    {
        GameObject newLog = Instantiate(debugLogPrefab, content);
        newLog.GetComponent<TextMeshProUGUI>().text = log;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}