using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [Isometric] Aturan jarak penempatan.
/// Semua item yang bisa di-place (bibit &amp; Lampu Matahari) harus berjarak
/// minimal 1 sel (default 2 m) dari bangunan: toko, komposer, dan kolam.
/// Radius bangunan dihitung dari bounds renderer supaya jaraknya terasa
/// dari tepi bangunan, bukan dari titik pusatnya saja.
/// </summary>
public static class PlacementRules
{
    public const float ClearanceInCells = 1f;

    struct BuildingInfo
    {
        public Transform transform;
        public float radius;
        public string label;
    }

    static readonly List<BuildingInfo> buildings = new List<BuildingInfo>();
    static bool cached;

    public static void ClearCache()
    {
        buildings.Clear();
        cached = false;
    }

    static void EnsureCache()
    {
        if (cached) return;
        buildings.Clear();

        AddBuildings(Object.FindObjectsByType<ShopStation>(FindObjectsSortMode.None), "toko");
        AddBuildings(Object.FindObjectsByType<DecomposerStation>(FindObjectsSortMode.None), "komposer");
        AddBuildings(Object.FindObjectsByType<PondRefillStation>(FindObjectsSortMode.None), "kolam");

        cached = true;
    }

    static void AddBuildings(Component[] stations, string label)
    {
        for (int i = 0; i < stations.Length; i++)
        {
            if (stations[i] == null) continue;

            BuildingInfo info = new BuildingInfo();
            info.transform = stations[i].transform;
            info.radius = FootprintRadius(stations[i].gameObject);
            info.label = label;
            buildings.Add(info);
        }
    }

    static float FootprintRadius(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0) return 0f;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            if (renderers[i] != null) bounds.Encapsulate(renderers[i].bounds);

        return Mathf.Max(bounds.extents.x, bounds.extents.z);
    }

    /// <summary>
    /// Apakah posisi terlalu dekat dengan bangunan?
    /// </summary>
    public static bool IsTooCloseToBuilding(Vector3 worldPosition, float cellSize, out string label)
    {
        label = "";

        float clearance = Mathf.Max(0f, cellSize) * ClearanceInCells;
        if (clearance <= 0f) return false;

        EnsureCache();

        for (int i = 0; i < buildings.Count; i++)
        {
            BuildingInfo building = buildings[i];
            if (building.transform == null) continue;

            Vector3 delta = building.transform.position - worldPosition;
            delta.y = 0f;

            float minDistance = clearance + building.radius;
            if (delta.sqrMagnitude <= minDistance * minDistance)
            {
                label = building.label;
                return true;
            }
        }

        return false;
    }
}
