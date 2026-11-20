using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// [Isometric] Grid samar untuk menaruh pohon saat edit mode.
/// Grid mengikuti player (snap ke sel) supaya area tanam selalu di sekitar pemain.
/// </summary>
[DisallowMultipleComponent]
public class GridOverlay : MonoBehaviour
{
    public float areaSize = 100f;
    public float cellSize = 2f;
    public Color lineColor = new Color(0.55f, 1f, 0.65f, 0.13f);
    public float lineWidth = 0.035f;
    public float height = 0.02f;

    MeshRenderer meshRenderer;

    void Awake()
    {
        Build();
        SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        if (meshRenderer != null) meshRenderer.enabled = visible;
    }

    public void SetCenter(Vector3 center)
    {
        // Snap pusat grid ke kelipatan cellSize supaya garis grid benar-benar
        // berimpit dengan posisi snap saat menanam pohon.
        float size = cellSize > 0.01f ? cellSize : 1f;
        float x = Mathf.Round(center.x / size) * size;
        float z = Mathf.Round(center.z / size) * size;

        transform.position = new Vector3(x, height, z);
    }

    void Build()
    {
        if (cellSize <= 0.01f) cellSize = 1f;

        int lines = Mathf.Max(1, Mathf.RoundToInt(areaSize / cellSize));
        int halfLines = Mathf.Max(1, Mathf.RoundToInt(lines * 0.5f));
        float half = halfLines * cellSize;
        lines = halfLines * 2;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int i = 0; i <= lines; i++)
        {
            float offset = -half + i * cellSize;

            AddQuad(vertices, triangles, new Vector3(offset, 0f, -half),
                new Vector3(offset, 0f, half), lineWidth);

            AddQuad(vertices, triangles, new Vector3(-half, 0f, offset),
                new Vector3(half, 0f, offset), lineWidth);
        }

        Mesh mesh = new Mesh { name = "PlacementGrid" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();

        GameObject meshObject = new GameObject("Mesh", typeof(MeshFilter), typeof(MeshRenderer));
        meshObject.transform.SetParent(transform, false);
        meshObject.GetComponent<MeshFilter>().sharedMesh = mesh;

        meshRenderer = meshObject.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = CreateMaterial();
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    static void AddQuad(List<Vector3> vertices, List<int> triangles,
        Vector3 start, Vector3 end, float width)
    {
        Vector3 direction = (end - start).normalized;
        Vector3 side = new Vector3(-direction.z, 0f, direction.x) * (width * 0.5f);

        int index = vertices.Count;
        vertices.Add(start - side);
        vertices.Add(start + side);
        vertices.Add(end + side);
        vertices.Add(end - side);

        triangles.Add(index);
        triangles.Add(index + 1);
        triangles.Add(index + 2);
        triangles.Add(index);
        triangles.Add(index + 2);
        triangles.Add(index + 3);
    }

    Material CreateMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Standard");

        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", lineColor);
        material.color = lineColor;

        if (shader != null && shader.name.Contains("Universal Render Pipeline/Unlit"))
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
        }

        material.renderQueue = 3000;
        return material;
    }
}
