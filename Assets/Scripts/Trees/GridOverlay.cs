using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// [Isometric] Grid penanda area tanam saat edit mode.
/// Tiap sel diwarnai hijau kalau bibit boleh ditanam di sana, merah kalau tidak
/// (belum tersinari Lampu Matahari / terlalu dekat pohon atau player).
/// Grid statis di tengah area dan mengikuti tinggi permukaan tanah, jadi tidak
/// tenggelam di tanjakan / bibir pulau.
/// </summary>
[DisallowMultipleComponent]
public class GridOverlay : MonoBehaviour
{
    [Header("Ukuran")]
    public float areaSize = 56f;
    public float cellSize = 2f;

    [Tooltip("Jarak quad grid di atas permukaan tanah (meter).")]
    public float height = 0.10f;

    [Header("Warna sel")]
    public Color validColor = new Color(0.06f, 0.78f, 0.20f, 0.34f);
    public Color invalidColor = new Color(0.78f, 0.10f, 0.12f, 0.32f);

    [Tooltip("Jarak antar sel supaya garis grid tetap terlihat.")]
    public float cellInset = 0.06f;

    [Header("Aturan penanaman")]
    public SunLampPlacer lampPlacer;
    public PestDefenseGame game;
    public Transform player;
    public float minTreeSpacing = 1.8f;
    public float minDistanceFromPlayer = 2f;

    /// <summary>Diisi TreeEditController saat item Lampu Matahari dipilih.</summary>
    [HideInInspector] public bool lampMode;

    readonly List<Vector3> cellCenters = new List<Vector3>();
    readonly List<bool> cellValidity = new List<bool>();

    // Tinggi 4 sudut tiap sel (x-,z-), (x-,z+), (x+,z+), (x+,z-).
    readonly List<Vector4> cellCorners = new List<Vector4>();
    readonly List<MeshRenderer> renderers = new List<MeshRenderer>();

    MeshFilter validFilter;
    MeshFilter invalidFilter;
    int lastHash = -1;

    void Awake()
    {
        Build();
        SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        for (int i = 0; i < renderers.Count; i++)
            if (renderers[i] != null) renderers[i].enabled = visible;
    }

    /// <summary>
    /// Grid sekarang statis. Method ini dipertahankan supaya pemanggil lama tidak error,
    /// tapi tidak menggeser grid lagi.
    /// </summary>
    public void SetCenter(Vector3 center)
    {
        // sengaja tidak melakukan apa-apa
    }

    public void Rebuild()
    {
        Build();
        SetVisible(false);
    }

    /// <summary>Hitung ulang warna sel. Dipanggil TreeEditController saat edit mode.</summary>
    public void RefreshValidity(bool force = false)
    {
        if (validFilter == null || invalidFilter == null || cellCenters.Count == 0) return;

        cellValidity.Clear();
        int hash = 17;

        for (int i = 0; i < cellCenters.Count; i++)
        {
            bool valid = IsValid(cellCenters[i]);
            cellValidity.Add(valid);
            hash = hash * 31 + (valid ? 1 : 0);
        }

        if (!force && hash == lastHash) return;
        lastHash = hash;

        List<Vector3> validVerts = new List<Vector3>(cellCenters.Count * 4);
        List<int> validTris = new List<int>(cellCenters.Count * 6);
        List<Vector3> invalidVerts = new List<Vector3>(cellCenters.Count * 4);
        List<int> invalidTris = new List<int>(cellCenters.Count * 6);

        float half = Mathf.Max(0.05f, cellSize * 0.5f - Mathf.Max(0f, cellInset) * 0.5f);

        for (int i = 0; i < cellCenters.Count; i++)
        {
            Vector3 center = cellCenters[i];
            bool valid = cellValidity[i];
            float lift = height + (valid ? 0f : 0.004f);

            List<Vector3> verts = valid ? validVerts : invalidVerts;
            List<int> tris = valid ? validTris : invalidTris;
            Vector4 corners = i < cellCorners.Count ? cellCorners[i] : Vector4.zero;

            AddQuad(verts, tris, center.x, center.z, half, corners, lift);
        }

        AssignMesh(validFilter, validVerts, validTris);
        AssignMesh(invalidFilter, invalidVerts, invalidTris);
    }

    bool IsValid(Vector3 localCell)
    {
        Vector3 world = transform.TransformPoint(localCell);

        // Jarak minimal 1 kotak dari toko / komposer / kolam berlaku untuk
        // semua item, termasuk saat memasang lampu.
        string building;
        if (PlacementRules.IsTooCloseToBuilding(world, cellSize, out building)) return false;

        // Saat memasang/ambil lampu, selain aturan bangunan semua sel boleh diketuk.
        if (lampMode) return true;

        if (lampPlacer == null) return true;
        if (!lampPlacer.IsIlluminated(world)) return false;

        if (game != null)
        {
            var trees = game.AllTrees;
            float minSqr = minTreeSpacing * minTreeSpacing;
            for (int i = 0; i < trees.Count; i++)
            {
                TreeDurability tree = trees[i];
                if (tree == null) continue;

                Vector3 delta = tree.transform.position - world;
                delta.y = 0f;
                if (delta.sqrMagnitude < minSqr) return false;
            }
        }

        if (player != null)
        {
            Vector3 delta = player.position - world;
            delta.y = 0f;
            if (delta.sqrMagnitude < minDistanceFromPlayer * minDistanceFromPlayer)
                return false;
        }

        return true;
    }

    void Build()
    {
        if (cellSize <= 0.01f) cellSize = 2f;

        PlacementRules.ClearCache();

        // Penting: buang sisa sel dari scene lama / build sebelumnya supaya
        // tidak ada dua grid yang saling menumpuk.
        ClearChildren();

        int lines = Mathf.Max(2, Mathf.RoundToInt(areaSize / cellSize));
        if (lines % 2 != 0) lines++;

        // Posisi grid di-snap ke ukuran sel. Y selalu 0 karena tinggi tanah
        // dibakar langsung ke vertex quad, bukan lewat transform.
        transform.position = new Vector3(
            Mathf.Round(transform.position.x / cellSize) * cellSize,
            0f,
            Mathf.Round(transform.position.z / cellSize) * cellSize);
        transform.rotation = Quaternion.identity;
        Physics.SyncTransforms();

        float extent = lines * cellSize * 0.5f;
        float half = Mathf.Max(0.05f, cellSize * 0.5f - Mathf.Max(0f, cellInset) * 0.5f);

        cellCenters.Clear();
        cellCorners.Clear();
        cellValidity.Clear();

        for (int x = 0; x < lines; x++)
        {
            for (int z = 0; z < lines; z++)
            {
                float cx = -extent + x * cellSize + cellSize * 0.5f;
                float cz = -extent + z * cellSize + cellSize * 0.5f;

                Vector3 worldCenter = transform.TransformPoint(new Vector3(cx, 0f, cz));
                float centerY = SampleGroundHeight(worldCenter, 0f);
                cellCenters.Add(new Vector3(cx, centerY, cz));

                float a = SampleGroundHeight(
                    transform.TransformPoint(new Vector3(cx - half, 0f, cz - half)), centerY);
                float b = SampleGroundHeight(
                    transform.TransformPoint(new Vector3(cx - half, 0f, cz + half)), centerY);
                float c = SampleGroundHeight(
                    transform.TransformPoint(new Vector3(cx + half, 0f, cz + half)), centerY);
                float d = SampleGroundHeight(
                    transform.TransformPoint(new Vector3(cx + half, 0f, cz - half)), centerY);

                cellCorners.Add(new Vector4(a, b, c, d));
            }
        }

        renderers.Clear();
        validFilter = CreateCellObject("ValidCells", validColor);
        invalidFilter = CreateCellObject("InvalidCells", invalidColor);

        lastHash = -1;
        RefreshValidity(true);
    }

    void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (child == null) continue;

            child.SetActive(false);

            MeshFilter filter = child.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
            {
                Mesh mesh = filter.sharedMesh;
                filter.sharedMesh = null;
                if (Application.isPlaying) Destroy(mesh);
                else DestroyImmediate(mesh);
            }

            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
    }

    MeshFilter CreateCellObject(string name, Color color)
    {
        GameObject child = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        child.hideFlags = HideFlags.DontSave;
        child.transform.SetParent(transform, false);
        child.transform.localPosition = Vector3.zero;

        MeshFilter filter = child.GetComponent<MeshFilter>();
        Mesh mesh = new Mesh { name = "Grid_" + name };
        mesh.hideFlags = HideFlags.DontSave;
        filter.sharedMesh = mesh;

        MeshRenderer renderer = child.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = CreateMaterial(color);
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderers.Add(renderer);
        return filter;
    }

    static void AssignMesh(MeshFilter filter, List<Vector3> vertices, List<int> triangles)
    {
        Mesh mesh = filter.sharedMesh;
        if (mesh == null) return;

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
    }

    static void AddQuad(List<Vector3> vertices, List<int> triangles,
        float centerX, float centerZ, float half, Vector4 corners, float lift)
    {
        int index = vertices.Count;

        vertices.Add(new Vector3(centerX - half, corners.x + lift, centerZ - half));
        vertices.Add(new Vector3(centerX - half, corners.y + lift, centerZ + half));
        vertices.Add(new Vector3(centerX + half, corners.z + lift, centerZ + half));
        vertices.Add(new Vector3(centerX + half, corners.w + lift, centerZ - half));

        triangles.Add(index);
        triangles.Add(index + 1);
        triangles.Add(index + 2);
        triangles.Add(index);
        triangles.Add(index + 2);
        triangles.Add(index + 3);
    }

    Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.hideFlags = HideFlags.DontSave;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        material.color = color;

        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetOverrideTag("RenderType", "Transparent");

        // ZTest default (LEqual): grid tertutup objek di depannya, tidak lagi
        // menimpa semuanya seperti sebelumnya.
        material.renderQueue = (int)RenderQueue.Transparent;
        return material;
    }

    /// <summary>
    /// Tinggi permukaan tanah di sebuah titik dunia. Hanya menerima permukaan
    /// tanah (mesh collider / objek ground_main), bukan pohon, kolam, atau
    /// bangunan di atasnya.
    /// </summary>
    public static float SampleGroundHeight(Vector3 worldPosition, float fallback = 0f)
    {
        const float startHeight = 40f;
        const float castDistance = 100f;

        Vector3 origin = new Vector3(worldPosition.x, startHeight, worldPosition.z);
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, castDistance,
            ~0, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return fallback;

        float namedGround = float.NaN;
        float meshGround = float.NaN;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider collider = hits[i].collider;
            if (collider == null) continue;

            if (collider.gameObject.name == "ground_main")
            {
                if (float.IsNaN(namedGround) || hits[i].point.y > namedGround)
                    namedGround = hits[i].point.y;
            }
            else if (collider is MeshCollider)
            {
                if (float.IsNaN(meshGround) || hits[i].point.y > meshGround)
                    meshGround = hits[i].point.y;
            }
        }

        if (!float.IsNaN(namedGround)) return namedGround;
        if (!float.IsNaN(meshGround)) return meshGround;
        return fallback;
    }
}
