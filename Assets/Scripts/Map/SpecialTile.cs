using UnityEngine;

public enum EventType
{
    None,
    StaminaDown,
    ItemGet,
    StaminaUp
}

public class SpecialTile : MonoBehaviour
{
    public EventType eventType;

    public void TriggerEvent()
    {
        switch (eventType)
        {
            case EventType.StaminaDown:
                Debug.Log("플레이어 스테미너 감소!");
                break;
            case EventType.ItemGet:
                Debug.Log("아이템 획득!");
                break;
            case EventType.StaminaUp:
                Debug.Log("플레이어 스테미너 증가!");
                break;
        }
    }
}
