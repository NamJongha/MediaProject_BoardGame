using Fusion;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SpecialTile : MonoBehaviour
{
    public SpecialTileEffectBase effect;
    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        Player player = other.GetComponent<Player>();
        if (player == null) return;

        if (effect != null)
        {
            effect.ApplyEffect(player);
            triggered = true;
        }
    }

    public void ResetTrigger()
    {
        triggered = false;
    }
}