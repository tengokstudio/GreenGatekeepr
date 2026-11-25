using UnityEngine;

/// <summary>
/// [General] Kabut di dunia adalah polusi udara. Makin banyak pohon yang ditanam
/// player dan masih hidup, makin rendah densitas kabut dan makin terang warnanya.
/// Jumlah target bisa diatur lewat <see cref="targetTrees"/>.
/// </summary>
[DisallowMultipleComponent]
public class PollutionFogController : MonoBehaviour
{
    [Header("Polusi")]
    public int targetTrees = 20;
    public float updateInterval = 0.5f;

    [Header("Kabut tebal (awal)")]
    public float pollutedDensity = 0.028f;
    public Color pollutedColor = new Color(0.38f, 0.37f, 0.32f, 1f);
    public float pollutedAmbient = 0.62f;

    [Header("Udara bersih (hutan penuh)")]
    public float clearDensity = 0.004f;
    public Color clearColor = new Color(0.60f, 0.66f, 0.63f, 1f);
    public float clearAmbient = 0.95f;

    [Header("Cahaya matahari")]
    public Light sunLight;
    public float pollutedSunIntensity = 0.55f;
    public float clearSunIntensity = 1f;

    /// <summary>0 = polusi parah, 1 = hutan penuh.</summary>
    public float Progress { get; private set; }

    float nextUpdate;

    void Start()
    {
        if (sunLight == null)
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].type == LightType.Directional)
                {
                    sunLight = lights[i];
                    break;
                }
            }
        }

        Apply(0f);
        nextUpdate = Time.time;
    }

    void Update()
    {
        if (Time.time < nextUpdate) return;
        nextUpdate = Time.time + Mathf.Max(0.1f, updateInterval);

        int planted = CountLivingPlantedTrees();
        float target = Mathf.Clamp01(planted / (float)Mathf.Max(1, targetTrees));

        // Transisi halus supaya perubahan kabut tidak mendadak.
        Progress = Mathf.MoveTowards(Progress, target, 0.06f);

        Apply(Progress);
    }

    static int CountLivingPlantedTrees()
    {
        PestDefenseGame game = PestDefenseGame.Instance;
        if (game == null) return 0;

        int count = 0;
        var trees = game.AllTrees;
        for (int i = 0; i < trees.Count; i++)
            if (trees[i] != null && trees[i].plantedByPlayer && trees[i].IsAlive) count++;

        return count;
    }

    void Apply(float t)
    {
        RenderSettings.fogDensity = Mathf.Lerp(pollutedDensity, clearDensity, t);
        RenderSettings.fogColor = Color.Lerp(pollutedColor, clearColor, t);
        RenderSettings.ambientIntensity = Mathf.Lerp(pollutedAmbient, clearAmbient, t);

        if (sunLight != null)
            sunLight.intensity = Mathf.Lerp(pollutedSunIntensity, clearSunIntensity, t);
    }
}
