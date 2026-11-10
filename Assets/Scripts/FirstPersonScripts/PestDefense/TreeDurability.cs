using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Durability pohon. Menerima damage dari hama (dan peluru), menampilkan health bar
/// 3D di atas pohon, lalu roboh + hilang saat HP habis.
/// </summary>
[DisallowMultipleComponent]
public class TreeDurability : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHealth = 150f;

    [Header("Visual - Health Bar (opsional)")]
    public GameObject healthBarRoot;
    public Transform healthBarFill;

    [Tooltip("Renderer daun yang warnanya ikut berubah saat HP turun.")]
    public Renderer[] foliageRenderers;
    public Color healthyColor = new Color(0.32f, 0.72f, 0.28f);
    public Color damagedColor = new Color(0.55f, 0.42f, 0.12f);

    [Header("Destroyed")]
    [Tooltip("Berapa lama pohon yang sudah roboh tetap ada sebelum dihapus.")]
    public float destroyedLifetime = 5f;
    public float fallDuration = 0.85f;
    public float fallAngle = 82f;

    [Header("Inventory Loot (General)")]
    [Tooltip("Item yang masuk inventory saat pohon ini hancur.")]
    public string lootItemId = "tree_oak";
    public bool giveLootToInventory = true;

    [Tooltip("True kalau pohon ini ditanam player lewat edit mode (bisa dihapus lagi).")]
    public bool plantedByPlayer;

    [Tooltip("Kalau false, health bar baru muncul setelah pohon kena damage.")]
    public bool alwaysShowHealthBar = true;

    [Header("Events")]
    public UnityEvent onDamaged;
    public UnityEvent onDestroyed;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    float currentHealth;
    Vector3 fillBaseScale;
    Vector3 fillBasePosition;
    MaterialPropertyBlock block;
    bool destroyed;
    bool lootOnDestroy = true;

    public float CurrentHealth => currentHealth;
    public float HealthRatio => maxHealth <= 0f ? 0f : Mathf.Clamp01(currentHealth / maxHealth);
    public bool IsAlive => !destroyed && currentHealth > 0f;

    void Awake()
    {
        currentHealth = maxHealth;

        if (healthBarFill != null)
        {
            fillBaseScale = healthBarFill.localScale;
            fillBasePosition = healthBarFill.localPosition;
        }

        RefreshVisuals();
    }

    public void ApplyDamage(float amount, Vector3 hitPoint)
    {
        ApplyDamage(amount, hitPoint, true);
    }

    /// <summary>
    /// <paramref name="allowLoot"/> = false dipakai hama, supaya pohon yang dihancurkan
    /// hama tidak masuk inventory.
    /// </summary>
    public void ApplyDamage(float amount, Vector3 hitPoint, bool allowLoot)
    {
        if (destroyed || amount <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        RefreshVisuals();
        onDamaged?.Invoke();

        if (currentHealth <= 0f)
        {
            lootOnDestroy = allowLoot;
            DestroyTree();
        }
    }

    void RefreshVisuals()
    {
        float ratio = HealthRatio;

        bool showBar = ratio > 0f && (alwaysShowHealthBar || ratio < 0.999f);
        if (healthBarRoot != null && healthBarRoot.activeSelf != showBar)
            healthBarRoot.SetActive(showBar);

        if (healthBarFill != null)
        {
            healthBarFill.localScale = new Vector3(fillBaseScale.x * ratio, fillBaseScale.y, fillBaseScale.z);
            healthBarFill.localPosition = fillBasePosition + new Vector3(-(1f - ratio) * fillBaseScale.x * 0.5f, 0f, 0f);
        }

        if (foliageRenderers == null || foliageRenderers.Length == 0) return;
        if (block == null) block = new MaterialPropertyBlock();

        Color color = Color.Lerp(damagedColor, healthyColor, ratio);
        for (int i = 0; i < foliageRenderers.Length; i++)
        {
            Renderer foliage = foliageRenderers[i];
            if (foliage == null) continue;

            foliage.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            foliage.SetPropertyBlock(block);
        }
    }

    void DestroyTree()
    {
        destroyed = true;
        currentHealth = 0f;
        RefreshVisuals();

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            if (colliders[i] != null) colliders[i].enabled = false;

        if (healthBarRoot != null) healthBarRoot.SetActive(false);

        if (giveLootToInventory && lootOnDestroy && !string.IsNullOrEmpty(lootItemId) && InventorySystem.Instance != null)
            InventorySystem.Instance.AddItem(lootItemId, 1);

        onDestroyed?.Invoke();
        StartCoroutine(FallRoutine());
    }

    IEnumerator FallRoutine()
    {
        Quaternion startRotation = transform.rotation;
        Vector3 startPosition = transform.position;
        Vector3 fallAxis = transform.right;
        if (Random.value < 0.5f) fallAxis = -fallAxis;

        float time = 0f;
        while (time < fallDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / fallDuration);
            float eased = t * t;

            transform.rotation = Quaternion.AngleAxis(fallAngle * eased, fallAxis) * startRotation;
            transform.position = startPosition + Vector3.down * (0.35f * eased);
            yield return null;
        }

        yield return new WaitForSeconds(Mathf.Max(0f, destroyedLifetime - fallDuration));
        Destroy(gameObject);
    }
}
