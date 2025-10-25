using UnityEngine;

public class SpawnPointChecker : MonoBehaviour
{
    [SerializeField] private bool isCharacterSpawnedOn = false;

    public void setSpawned()
    {
        isCharacterSpawnedOn = true;
    }

    public void deSpawned()
    {
        isCharacterSpawnedOn = false;
    }

    public bool getSpawned()
    {
        return isCharacterSpawnedOn;
    }
}
