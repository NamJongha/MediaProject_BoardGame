using Fusion;
using Unity;

public struct NetworkPlayerData : INetworkStruct
{
    public NetworkString<_16> playerName { get; set; }
    [Networked] public int stamina { get; set; }
}
