using Fusion;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Player : NetworkBehaviour
{
    private NetworkCharacterController _cc;

    [Networked] public bool isReady { get; set; }

    [Networked] public bool isPlayerTurn { get; set; }

    private int diceNum = 0;

    private ChangeDetector changeDetector;
    private TurnManager turnManager;

    [SerializeField] private Button endTurnButtonPrefab;
    private Button endTurnButtonInstance;

    private void Awake()
    {
        _cc = GetComponent<NetworkCharacterController>();
    }

    public override void Spawned()
    {
        Debug.Log("Player just spawned");

        changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (Object.HasInputAuthority)
        {
            var lobby = FindFirstObjectByType<LobbyManager>();
            if (lobby != null)
            {
                Debug.Log("Found lobby manager");
                lobby.SetLocalPlayer(this);
                lobby.UpdateButtonState();
            }

            turnManager = FindFirstObjectByType<TurnManager>();

            endTurnButtonInstance = Instantiate(endTurnButtonPrefab);

            //버튼 parent 캔버스로 설정
            endTurnButtonInstance.transform.SetParent(GameObject.Find("Canvas").transform);

            //버튼 위치 조정
            RectTransform rt = endTurnButtonInstance.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(360, -150);

            //버튼 텍스트 설정
            endTurnButtonInstance.GetComponentInChildren<TMP_Text>().text = "End Turn";

            endTurnButtonInstance.onClick.AddListener(OnEndTurnButtonClicked);
            endTurnButtonInstance.gameObject.SetActive(false);
        }
    }

    public override void Render()
    {
        base.Render();
        foreach(var change in changeDetector.DetectChanges(this))
        {
            if(change == nameof(isReady))
            {
                Debug.Log("this player changed ready state");
            }
            if(change == nameof(isPlayerTurn))
            {
                Debug.Log("Player turn started");
                if(Object.HasInputAuthority && endTurnButtonPrefab != null)
                {
                    //플레이어 턴에만 버튼 표시
                    endTurnButtonInstance.gameObject.SetActive(isPlayerTurn);
                }
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
            if (GetInput(out NetworkInputData data))
            {
                if (isPlayerTurn)
                {
                    data.direction.Normalize();
                    _cc.Move(5 * data.direction * Runner.DeltaTime);
                }
                else
                {
                    Vector3 gravity = new Vector3(0, -1, 0);
                    _cc.Move(5 * gravity * Runner.DeltaTime);
                }
            }
    }

    private void OnEndTurnButtonClicked()
    {
        if (Object.HasInputAuthority)
        {
            if (isPlayerTurn)
            {
                RPC_RequestEndTurn();
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestEndTurn()
    {
        if (turnManager == null)
        {
            turnManager = FindFirstObjectByType<TurnManager>();
        }
        turnManager.OnPlayerEndTurn(Object.InputAuthority);
    }


    #region player ready in lobby
    public void ChangeReady()
    {
        if (Object.HasStateAuthority)
        {
            isReady = !isReady;
        }
        else
        {
            RPC_RequestChangeReady(!isReady);
        }
    }

    //isReady should be changed by the Host -> client send change to host with rpc
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestChangeReady(bool state)
    {
        isReady = state;
    }

    public void resetReady()
    {
        if (Object.HasStateAuthority)
        {
            if (Object.InputAuthority == Runner.LocalPlayer) isReady = true;
            else isReady = false;
        }
    }
    #endregion

    #region player movable while playing game
    public void ChangeIsPlayerTurn(bool state)
    {
        if ((Object.HasStateAuthority))
        {
            isPlayerTurn = state;
        }
        else
        {
            RPC_RequestChangeIsPlayerTurn(state);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestChangeIsPlayerTurn(bool state)
    {
        isPlayerTurn = state;
    }

    public void ResetIsPlayerTurn()
    {
        isPlayerTurn = false;
    }
    #endregion

    #region player dice logic
    public void RollTheDice()
    {
        int dice = Random.Range(1, 7);
        diceNum = dice;
    }

    public int GetDiceNum()
    {
        return diceNum;
    }
#endregion
}

//플레이어 턴에서 할 행동
//주사위 아이템 맵보기 중 하나 선택하기
//if 주사위
//  주사위 굴리기
//  나온 수만큼 전진
//  전진 중 갈림길 나오면 선택
//  갈림길 선택 후 다시 전진
//  이벤트칸 도착 시 이벤트
//  턴 종료
//else if 아이템
//  갖고 있는 아이템 중 하나 선택
//  아이템이 타겟형인 경우 플레이어 선택
//  선택한 아이템 능력 발동
//  주사위와 맵보기 중 선택
//else if 맵보기
//  플레이어 입력 수집
//  입력에 따라 카메라 이동
//  if 플레이어 맵보기 종료
//      주사위 아이템 맵보기 중 선택