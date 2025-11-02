using System;
using UnityEngine;
using Fusion;

public class LogManager : NetworkBehaviour
{
    public static LogManager Instance { get; private set; }
    public event Action<string> OnRecievedLog;

    private void Awake()
    {
        Instance = this;
    }

    public void Log(string log)
    {
        Debug.Assert(Runner != null);
        RPC_ShowLogToAll(log);
    }
    
    
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_ShowLogToAll(string log, RpcInfo info = default)
    {
        OnRecievedLog?.Invoke(log);
    }
}
