using UnityEngine;
using Fusion;
/*
public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    [SerializeField] private Transform[] spawnPoints;

    public void PlayerJoined(PlayerRef player)
    {
        if (Runner.IsServer)
        {
            int index = player.RawEncoded % spawnPoints.Length;
            Transform point = spawnPoints[index];

            NetworkObject playerObj = Runner.Spawn(
                GameManager.Instance.PlayerPrefab,
                point.position,
                point.rotation,
                player
            );

            // 🔥 스폰 지점에 연결된 BoardNode 가져오기
            BoardNode spawnNode = point.GetComponent<BoardNode>();
            PlayerMover mover = playerObj.GetComponent<PlayerMover>();

            // 🔥 currentNode를 반드시 설정해야 MoveSteps()가 정상동작
            mover.SetCurrentNode(spawnNode);
        }
    }
    
}
*/
