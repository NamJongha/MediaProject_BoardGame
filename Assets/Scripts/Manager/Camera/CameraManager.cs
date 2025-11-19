using System;
using Fusion;
using UnityEngine;

public class CameraManager : NetworkBehaviour
{
    public static CameraManager Instance { get; private set; }

    [SerializeField] private Camera mainCamera;
    
    [SerializeField] private Vector3 cameraOffset;
    [SerializeField] private Vector3 cameraRotation;
    [SerializeField] private float cameraSpeedOffset;

    private bool isSpawned = false;
    
    [Networked] public bool isWatchingPlayer { get; set; } = false;
    [Networked] public NetworkObject currentPlayer { get; set; }

    public override void Spawned()
    {
        isSpawned = true;
    }
    
    private void Awake()
    {
        Instance = this;
    }

    private void FixedUpdate()
    {
        if (!isSpawned) return;
        
        if (isWatchingPlayer)
        {
            Vector3 targetPosition = (currentPlayer.transform.position + cameraOffset);
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPosition, cameraSpeedOffset * Time.deltaTime);
            mainCamera.transform.rotation = Quaternion.Euler(cameraRotation);
        }
    }

    //
    public void SetPlayerCamera(NetworkObject playerObject)
    {
        RPC_RequestSetPlayerCamera(playerObject);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_RequestSetPlayerCamera(NetworkObject playerObject)
    {
        Instance.currentPlayer = playerObject;
        Instance.isWatchingPlayer = true;
    }

    //method to look at somewhere else
    public void SetPointCamera()
    {
        isWatchingPlayer = false;
    }
}
