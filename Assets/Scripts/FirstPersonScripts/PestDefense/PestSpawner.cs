using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sarang hama. Memunculkan satu hama setiap <see cref="spawnInterval"/> detik,
/// berhenti total setelah <see cref="spawnDuration"/> detik ATAU semua pohon habis.
/// </summary>
[DisallowMultipleComponent]
public class PestSpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject pestPrefab;
    public Transform pestParent;
    public PestDefenseGame game;

    [Header("Timing")]
    [Tooltip("Jeda sebelum hama pertama muncul (detik).")]
    public float firstSpawnDelay = 1f;

    [Tooltip("Jarak antar spawn (detik).")]
    public float spawnInterval = 5f;

    [Tooltip("Berapa lama sarang terus spawn sebelum berhenti (detik).")]
    public float spawnDuration = 60f;

    [Header("Placement")]
    [Tooltip("Hama muncul di dalam radius ini dari sarang (meter).")]
    public float spawnRadius = 1.6f;

    [Header("Events")]
    public PestSpawnedEvent onSpawned;

    [System.Serializable]
    public class PestSpawnedEvent : UnityEvent<PestEnemy> { }

    float startTime;
    float nextSpawnTime;
    float pauseStart;
    bool paused;
    bool stopped;
    int spawnedCount;

    public bool IsStopped => stopped;
    public int SpawnedCount => spawnedCount;

    void Start()
    {
        startTime = Time.time;
        nextSpawnTime = Time.time + Mathf.Max(0f, firstSpawnDelay);

        if (game == null) game = PestDefenseGame.Instance;
        if (pestParent == null)
        {
            GameObject parent = GameObject.Find("Pests");
            if (parent != null) pestParent = parent.transform;
        }
    }

    void Update()
    {
        if (game == null) game = PestDefenseGame.Instance;

        // [Isometric] Edit mode: spawner ikut pause, timer digeser saat resume.
        if (game != null && game.EditMode)
        {
            if (!paused)
            {
                paused = true;
                pauseStart = Time.time;
            }
            return;
        }

        if (paused)
        {
            float pauseDuration = Time.time - pauseStart;
            startTime += pauseDuration;
            nextSpawnTime += pauseDuration;
            paused = false;
        }

        if (stopped) return;

        if (Time.time - startTime >= spawnDuration)
        {
            StopSpawning("waktu habis");
            return;
        }

        if (game != null && game.AllTreesDestroyed)
        {
            StopSpawning("semua pohon hancur");
            return;
        }

        while (Time.time >= nextSpawnTime)
        {
            nextSpawnTime += Mathf.Max(0.1f, spawnInterval);

            // Jangan menumpuk spawn setelah frame yang sangat lambat.
            if (Time.time - startTime > spawnDuration) break;

            SpawnPest();
        }
    }

    void SpawnPest()
    {
        if (pestPrefab == null) return;

        Vector2 offset = Random.insideUnitCircle * spawnRadius;
        Vector3 position = transform.position + new Vector3(offset.x, 0f, offset.y);
        Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        GameObject pestObject = Instantiate(pestPrefab, position, rotation, pestParent);
        pestObject.name = name + "_Pest_" + (++spawnedCount);

        PestEnemy pest = pestObject.GetComponent<PestEnemy>();
        if (pest != null)
        {
            pest.Initialize(game);
            onSpawned?.Invoke(pest);
        }
    }

    public void StopSpawning(string reason)
    {
        if (stopped) return;

        stopped = true;
        Debug.Log("[PestSpawner] " + name + " berhenti spawn: " + reason +
                  " (total " + spawnedCount + " hama).");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
