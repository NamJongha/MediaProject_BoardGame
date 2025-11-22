using System;
using Fusion;
using UnityEngine;

public class CameraManager : NetworkBehaviour
{
    public static CameraManager Instance { get; private set; }

    [SerializeField] private Camera mainCamera;
    
    [SerializeField] private Vector3 playerViewCameraOffset = new Vector3(0, 25, -25);
    [SerializeField] private Vector3 playerViewCameraRotation = new Vector3(45, 0, 0);
    [SerializeField] private float cameraSpeedOffset;

    private bool isSpawned = false;
    private bool isViewMap = false;
    
    [Networked] public bool isWatchingPlayer { get; set; } = false;
    [Networked] public NetworkObject currentPlayer { get; set; }
    
    //View Map start point
    [SerializeField] private Vector3 viewMapStartPos = new Vector3(0, 120, 0);
    [SerializeField] private Vector3 viewMapStartRot = new Vector3(90, 0, 0);
    
    [Networked] public Vector3 networkMapCamPos { get; private set; }
    [Networked] public Vector3 networkMapCamRot { get; private set; }
    
    
    //Camera movement range
    [SerializeField] private Vector2 clampX = new Vector2(10, 1260);
    [SerializeField] private Vector2 clampZ = new Vector2(-60, 80);
    [SerializeField] private Vector2 clampY = new Vector2(80, 180);
    //Camera moving speed
    [SerializeField] private float mapMoveSpeed = 150f;
    [SerializeField] private float mapZoomSpeed = 300f;

    private bool isLocalPlayerTurn = false;

    public override void Spawned()
    {
        isSpawned = true;
        isWatchingPlayer = false;
        networkMapCamPos = new Vector3(0, 120, 0);
        networkMapCamRot = new Vector3(90, 0, 0);
    }
    
    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        
    }
    
    private void FixedUpdate()
    {
        if (!isSpawned) return;
        
        if (isWatchingPlayer)
        {
            Vector3 targetPosition = (currentPlayer.transform.position + playerViewCameraOffset);
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPosition, cameraSpeedOffset * Time.deltaTime);
            mainCamera.transform.rotation = Quaternion.Euler(playerViewCameraRotation);
        }
        
        if (isViewMap)
        {
            HandleViewMapControls();
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

    //to check if the user can move the camera(현재 턴이 아닌 플레이어는 못 누르도록 하기)
    public void SetIsLocalPlayerTurn(bool isLocalPlayerTurn)
    {
        this.isLocalPlayerTurn = isLocalPlayerTurn;
    }

    //method to look at somewhere else
    public void EnterViewMap()
    {
        isWatchingPlayer = false;
        isViewMap = true;
        Debug.Log("isLocalPlayerTurn:" + isLocalPlayerTurn);

        // 기본 위치로 이동
        mainCamera.transform.position = viewMapStartPos;
        mainCamera.transform.rotation = Quaternion.Euler(viewMapStartRot);
    }

    public void ExitViewMap()
    {
        isViewMap = false;
        isWatchingPlayer = true;
    }
    
    private void HandleViewMapControls()
    {
        if (isLocalPlayerTurn)
        {
            Vector3 pos = mainCamera.transform.position;

            // WASD 이동
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            pos += new Vector3(h, 0, v) * mapMoveSpeed * Time.deltaTime;

            // 마우스 드래그 이동
            if (Input.GetMouseButton(2))
            {
                pos -= mainCamera.transform.right * Input.GetAxis("Mouse X") * mapMoveSpeed * Time.deltaTime;
                pos -= mainCamera.transform.up * Input.GetAxis("Mouse Y") * mapMoveSpeed * Time.deltaTime;
            }

            // 마우스 휠 줌
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            pos += mainCamera.transform.forward * scroll * mapZoomSpeed * Time.deltaTime;

            // 범위 제한 (Clamp)
            pos.x = Mathf.Clamp(pos.x, clampX.x, clampX.y);
            pos.z = Mathf.Clamp(pos.z, clampZ.x, clampZ.y);
            pos.y = Mathf.Clamp(pos.y, clampY.x, clampY.y);

            mainCamera.transform.position = pos;
            
            RPC_UpdateCamera(pos, viewMapStartRot);
        }
        else
        {
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, networkMapCamPos, mapMoveSpeed * Runner.DeltaTime);
            mainCamera.transform.rotation = Quaternion.Euler(networkMapCamRot);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_UpdateCamera(Vector3 pos, Vector3 rot)
    {
        networkMapCamPos = pos;
        networkMapCamRot = rot;
    }

    //public void ToggleMapView()
    //{
    //    if (!isViewMap)
    //    {
    //        EnterViewMap();
    //    }
    //    else
    //    {
    //        ExitViewMap();
    //    }
    //}
}
