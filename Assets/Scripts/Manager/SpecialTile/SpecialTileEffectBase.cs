using Fusion;
using UnityEngine;

public abstract class SpecialTileEffectBase : ScriptableObject
{
    [TextArea] public string description;
    
    public abstract void ApplyEffect(Player target);
}