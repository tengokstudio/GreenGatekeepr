using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Hama yang menyerang pohon. Dia mencari pohon terdekat yang masih hidup, berjalan
/// mendekat, lalu menggigit berkala. Hama TIDAK PERNAH menyerang player.
///
/// [FPS] Saat mati, hama menjadi mayat yang bertahan <see cref="corpseLifetime"/> detik.
/// Kalau player mendekat, muncul tanda "AMBIL"; tap mayat untuk memasukkannya ke inventory.
/// </summary>
[DisallowMultipleComponent]
public class PestEnemy : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHealth = 30f;

    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float turnSpeed = 12f;

    [Header("Attack")]
    public float attackRange = 1.8f;
    public float attackInterval = 0.9f;
    public float damagePerBite = 4f;

    [Header("Lifetime")]
    [Tooltip("Jeda sebelum hama hilang setelah semua pohon hancur.")]
    public float despawnAfterTreesGone = 3f;

    [Tooltip("[FPS] Hama hidup despawn setelah umur ini (detik).")]
    public float maxLifetime = 120f;

    [Tooltip("[FPS] Hama hidup despawn setelah memakan sebanyak ini pohon.")]
    public int treesToEatBeforeDespawn = 5;

    [Header("Corpse & Pickup (FPS)")]
    public string lootItemId = "pest_corpse";
    public float corpseLifetime = 20f;
    public float pickupRange = 6f;

    [Header("Events")]
    public UnityEvent onAttackTree;
    public UnityEvent onDeath;

    static Material markerMaterial;

    PestDefenseGame game;
    TreeDurability target;
    Collider ownCollider;
    CameraSwitcher switcher;
    Transform player;
    GameObject marker;
    Vector3 markerBasePosition;
    Vector3 corpseStartScale;
    Quaternion corpseStartRotation;
    Vector3 approachPoint;
    float currentHealth;
    float nextAttackTime;
    float treesGoneAt = -1f;
    float corpseAge;
    float aliveTime;
    int treesEaten;
    bool dead;
    bool pickedUp;
    bool despawning;

    public bool IsAlive => !dead && !despawning && gameObject.activeInHierarchy;
    public bool IsCorpse => dead && !pickedUp;

    void Awake()
    {
        currentHealth = maxHealth;
        ownCollider = GetComponent<Collider>();
        if (game == null) game = PestDefenseGame.Instance;
        switcher = FindFirstObjectByType<CameraSwitcher>();
    }

    public void Initialize(PestDefenseGame owner)
    {
        game = owner;
        if (game != null) game.RegisterPest(this);
    }

    void Start()
    {
        if (game == null) game = PestDefenseGame.Instance;
        if (game != null) game.RegisterPest(this);
        if (switcher == null) switcher = FindFirstObjectByType<CameraSwitcher>();

        IgnorePlayerCollision();
        PickTarget();
    }

    void IgnorePlayerCollision()
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null) player = playerObject.transform;

        if (ownCollider == null || playerObject == null) return;

        Collider[] playerColliders = playerObject.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < playerColliders.Length; i++)
        {
            if (playerColliders[i] != null)
                Physics.IgnoreCollision(ownCollider, playerColliders[i], true);
        }
    }

    void Update()
    {
        if (game == null) game = PestDefenseGame.Instance;

        if (dead)
        {
            UpdateCorpse();
            return;
        }

        if (despawning) return;

        // [Isometric] Edit mode mem-pause seluruh AI hama.
        if (game != null && game.EditMode) return;

        // [FPS] Hama hidup punya batas umur.
        aliveTime += Time.deltaTime;
        if (maxLifetime > 0f && aliveTime >= maxLifetime)
        {
            StartDespawn();
            return;
        }

        // Semua pohon sudah habis: hama berkeliaran sebentar lalu pergi.
        if (game != null && game.AllTreesDestroyed)
        {
            if (treesGoneAt < 0f) treesGoneAt = Time.time;
            if (Time.time - treesGoneAt >= despawnAfterTreesGone) StartDespawn();
            return;
        }

        if (target == null || !target.IsAlive) PickTarget();
        if (target == null) return;

        MoveToTree();
        TryAttack();
    }

    void PickTarget()
    {
        target = game != null ? game.GetNearestTree(transform.position) : null;
        if (target == null) return;

        float angle = Random.Range(0f, Mathf.PI * 2f);
        float radius = Mathf.Max(0.6f, attackRange * 0.8f);
        approachPoint = target.transform.position +
                        new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
    }

    void MoveToTree()
    {
        Vector3 goal = approachPoint;
        goal.y = transform.position.y;

        Vector3 toGoal = goal - transform.position;
        toGoal.y = 0f;

        float distance = toGoal.magnitude;
        if (distance > 0.08f)
        {
            float step = Mathf.Min(moveSpeed * Time.deltaTime, distance);
            transform.position += toGoal.normalized * step;

            Quaternion look = Quaternion.LookRotation(toGoal.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
        }
    }

    void TryAttack()
    {
        if (target == null || !target.IsAlive) return;

        float distanceToTree = Vector3.Distance(transform.position, target.transform.position);
        if (distanceToTree > attackRange + 0.35f) return;
        if (Time.time < nextAttackTime) return;

        nextAttackTime = Time.time + Mathf.Max(0.1f, attackInterval);
        target.ApplyDamage(damagePerBite, target.transform.position, false);
        onAttackTree?.Invoke();

        // [FPS] Hitung pohon yang hancur karena gigitan hama ini.
        if (!target.IsAlive)
        {
            treesEaten++;
            if (treesToEatBeforeDespawn > 0 && treesEaten >= treesToEatBeforeDespawn)
            {
                StartDespawn();
                return;
            }
        }

        StartCoroutine(BiteRoutine());
    }

    IEnumerator BiteRoutine()
    {
        Vector3 baseScale = transform.localScale;
        transform.localScale = new Vector3(baseScale.x * 1.15f, baseScale.y * 0.8f, baseScale.z * 1.15f);
        yield return new WaitForSeconds(0.12f);
        if (transform != null) transform.localScale = baseScale;
    }

    public void ApplyDamage(float amount, Vector3 hitPoint)
    {
        if (dead || despawning || amount <= 0f) return;

        currentHealth -= amount;
        if (currentHealth > 0f) return;

        Die();
    }

    void Die()
    {
        dead = true;
        corpseAge = 0f;
        corpseStartScale = transform.localScale;
        corpseStartRotation = transform.rotation;

        if (game != null) game.OnPestKilled(this);
        onDeath?.Invoke();

        CreateMarker();
        StartCoroutine(CorpseRoutine());
    }

    IEnumerator CorpseRoutine()
    {
        Vector3 targetScale = new Vector3(
            corpseStartScale.x * 1.2f,
            corpseStartScale.y * 0.4f,
            corpseStartScale.z * 1.2f);

        float time = 0f;
        const float duration = 0.35f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            transform.localScale = Vector3.Lerp(corpseStartScale, targetScale, t);
            transform.rotation = corpseStartRotation * Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 12f, t));
            yield return null;
        }

        transform.localScale = targetScale;
    }

    void UpdateCorpse()
    {
        if (pickedUp) return;

        // Edit mode pause juga menahan umur mayat.
        if (game != null && game.EditMode)
        {
            if (marker != null) marker.SetActive(false);
            return;
        }

        corpseAge += Time.deltaTime;

        bool firstPerson = switcher != null && switcher.CurrentMode == CameraSwitcher.Mode.FirstPerson;
        bool near = player != null &&
                    HorizontalDistance(player.position, transform.position) <= pickupRange;

        if (marker != null)
        {
            bool show = firstPerson && near;
            if (marker.activeSelf != show) marker.SetActive(show);

            if (show)
            {
                marker.transform.position = markerBasePosition +
                    Vector3.up * (0.07f * Mathf.Sin(Time.time * 4f));
            }
        }

        if (corpseAge >= corpseLifetime) DespawnCorpse();
    }

    /// <summary>Dipanggil oleh WorldTapRouter saat mayat di-tap di mode FPS.</summary>
    public bool TryPickup()
    {
        if (!dead || pickedUp || despawning) return false;
        if (switcher == null || switcher.CurrentMode != CameraSwitcher.Mode.FirstPerson) return false;
        if (player == null) return false;
        if (HorizontalDistance(player.position, transform.position) > pickupRange) return false;
        if (InventorySystem.Instance == null) return false;
        if (!InventorySystem.Instance.AddItem(lootItemId, 1)) return false;

        pickedUp = true;
        if (marker != null) marker.SetActive(false);
        if (ownCollider != null) ownCollider.enabled = false;
        StartCoroutine(PickupRoutine());
        return true;
    }

    IEnumerator PickupRoutine()
    {
        Vector3 startScale = transform.localScale;
        float time = 0f;
        const float duration = 0.18f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            transform.localScale = Vector3.Lerp(startScale, startScale * 1.25f, t);
            yield return null;
        }

        time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            transform.localScale = Vector3.Lerp(startScale * 1.25f, Vector3.zero, t);
            yield return null;
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// [FPS] Hama yang masih hidup pergi tanpa meninggalkan mayat. Dipakai saat umur
    /// habis atau setelah memakan cukup banyak pohon.
    /// </summary>
    public void StartDespawn()
    {
        if (dead || despawning) return;

        despawning = true;
        target = null;
        if (game != null) game.UnregisterPest(this);
        if (ownCollider != null) ownCollider.enabled = false;
        StartCoroutine(DespawnRoutine());
    }

    void DespawnCorpse()
    {
        if (pickedUp) return;

        pickedUp = true;
        if (marker != null) marker.SetActive(false);
        if (ownCollider != null) ownCollider.enabled = false;
        StartCoroutine(DespawnRoutine());
    }

    IEnumerator DespawnRoutine()
    {
        Vector3 startScale = transform.localScale;
        float time = 0f;
        const float duration = 0.6f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            transform.position += Vector3.down * (Time.deltaTime * 0.25f);
            yield return null;
        }

        Destroy(gameObject);
    }

    void CreateMarker()
    {
        if (marker != null) return;

        markerBasePosition = transform.position + Vector3.up * 1.15f;
        marker = new GameObject("PickupMarker");
        marker.transform.position = markerBasePosition;

        TextMesh text = marker.AddComponent<TextMesh>();
        text.text = "AMBIL";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 72;
        text.characterSize = 0.05f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontStyle = FontStyle.Bold;
        text.color = new Color(1f, 0.92f, 0.35f);

        GameObject diamond = GameObject.CreatePrimitive(PrimitiveType.Cube);
        diamond.name = "Diamond";
        diamond.transform.SetParent(marker.transform, false);
        diamond.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        diamond.transform.localScale = Vector3.one * 0.16f;
        diamond.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);

        Collider diamondCollider = diamond.GetComponent<Collider>();
        if (diamondCollider != null) Destroy(diamondCollider);

        Renderer diamondRenderer = diamond.GetComponent<Renderer>();
        if (diamondRenderer != null) diamondRenderer.sharedMaterial = MarkerMaterial;

        marker.AddComponent<HealthBarBillboard>();
        marker.SetActive(false);
    }

    void OnDestroy()
    {
        if (marker != null) Destroy(marker);
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    static Material MarkerMaterial
    {
        get
        {
            if (markerMaterial != null) return markerMaterial;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return null;

            markerMaterial = new Material(shader);
            Color color = new Color(1f, 0.85f, 0.25f);
            if (markerMaterial.HasProperty("_BaseColor")) markerMaterial.SetColor("_BaseColor", color);
            markerMaterial.color = color;
            markerMaterial.EnableKeyword("_EMISSION");
            if (markerMaterial.HasProperty("_EmissionColor"))
                markerMaterial.SetColor("_EmissionColor", color * 1.5f);

            return markerMaterial;
        }
    }
}
