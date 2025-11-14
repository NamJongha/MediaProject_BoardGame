using UnityEngine;

public static class UtilityHelper
{
    public static float GetTileSize(GameObject prefab)
    {
        if (prefab == null) return 1f;
        Renderer r = prefab.GetComponentInChildren<Renderer>();
        return r != null ? r.bounds.size.x : 1f;
    }

    public static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
    }
}