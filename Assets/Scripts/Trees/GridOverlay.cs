using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// [Isometric] Grid penanda area tanam saat edit mode.
/// Lebih jelas dari versi lama: garis utama tiap 5 sel, titik di tengah tiap sel
/// (posisi pohon ditanam), dan warna lebih terang.
/// </summary>
[DisallowMultipleComponent]
public class GridOverlay : MonoBehaviour
{
    [Header("Ukuran")]
    public float areaSize = 100f;
    public float cellSize = 2f;
    public float height = 0.03f;

    [Header("Garis")]
    public Color lineColor = new Color(0.55f, 1f, 0.65f, 0.30f);
    public float lineWidth = 0.045f;

    public Color majorLineColor = new Color(0.55f, 1f, 0.65f, 0.65f);
    public float majorLineWidth = 0.10f;
    public int majorEvery = 5;

    [Header("Titik tengah sel")]
    public bool showCenterDots = true;
    public Color centerDotColor = new Color(0.82f, 1f, 0.86f, 0.50f);
    public float centerDotSize = 0.18f;

    readonly List<MeshRenderer> renderers = new List<MeshRenderer>();

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
        if (majorEvery < 1) majorEvery = 5;

        int lines = Mathf.Max(1, Mathf.RoundToInt(areaSize / cellSize));
        int halfLines = Mathf.Max(1, Mathf.RoundToInt(lines * 0.5f));
        float half = halfLines * cellSize;
        lines = halfLines * 2;

        List<Vector3> normalVertices = new List<Vector3>();
        List<int> normalTriangles = new List<int>();
        List<Vector3> majorVertices = new List<Vector3>();
        List<int> majorTriangles = new List<int>();
        List<Vector3> dotVertices = new List<Vector3>();
        List<int> dotTriangles = new List<int>();

        for (int i = 0; i <= lines; i++)
        {
            float offset = -half + i * cellSize;
            bool major = i % majorEvery == 0;

            List<Vector3> vertices = major ? majorVertices : normalVertices;
            List<int> triangles = major ? majorTriangles : normalTriangles;
            float width = major ? majorLineWidth : lineWidth;

            AddQuad(vertices, triangles, new Vector3(offset, 0f, -half),
                new Vector3(offset, 0f, half), width);

            AddQuad(vertices, triangles, new Vector3(-half, 0f, offset),
                new Vector3(half, 0f, offset), width);
        }

        if (showCenterDots)
        {
            float dot = Mathf.Max(0.02f, centerDotSize);
            for (int x = 0; x < lines; x++)
            {
                for (int z = 0; z < lines; z++)
                {
                    float cx = -half + x * cellSize + cellSize * 0.5f;
                    float cz = -half + z * cellSize + cellSize * 0.5f;
                    AddDot(dotVertices, dotTriangles, new Vector3(cx, 0f, cz), dot);
                }
            }
        }

        renderers.Clear();
        CreateMeshObject("Lines", normalVertices, normalTriangles, lineColor, 0f);
        CreateMeshObject("MajorLines", majorVertices, majorTriangles, majorLineColor, 0.002f);
        if (showCenterDots)
            CreateMeshObject("CenterDots", dotVertices, dotTriangles, centerDotColor, 0.004f);
    }

    void CreateMeshObject(string name, List<Vector3> vertices, List<int> triangles,
        Color color, float yOffset)
    {
        if (vertices.Count == 0 || triangles.Count == 0) return;

        Mesh mesh = new Mesh { name = "PlacementGrid_" + name };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();

        GameObject meshObject = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        meshObject.transform.SetParent(transform, false);
        meshObject.transform.localPosition = new Vector3(0f, yOffset, 0f);
        meshObject.GetComponent<MeshFilter>().sharedMesh = mesh;

        MeshRenderer renderer = meshObject.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = CreateMaterial(color);
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderers.Add(renderer);
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

    static void AddDot(List<Vector3> vertices, List<int> triangles, Vector3 center, float size)
    {
        float half = size * 0.5f;
        int index = vertices.Count;

        vertices.Add(center + new Vector3(-half, 0f, -half));
        vertices.Add(center + new Vector3(-half, 0f, half));
        vertices.Add(center + new Vector3(half, 0f, half));
        vertices.Add(center + new Vector3(half, 0f, -half));

        triangles.Add(index);
        triangles.Add(index + 1);
        triangles.Add(index + 2);
        triangles.Add(index);
        triangles.Add(index + 2);
        triangles.Add(index + 3);
    }

    Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Standard");

        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        material.color = color;

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
