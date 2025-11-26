using Fusion;
using UnityEngine;

public class BoardSeedNetworkObj : NetworkBehaviour
{
    [Networked]
    public int Seed { get; set; }
}