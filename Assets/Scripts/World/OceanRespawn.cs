using Unity.Cinemachine.Samples;
using UnityEngine;

/// <summary>
/// [General] Laut mengelilingi pulau. Kalau player jatuh ke laut (turun di bawah
/// <see cref="seaLevel"/>), dia dikembalikan ke posisi spawn pertama dengan
/// kecepatan gerak di-reset.
/// </summary>
[DisallowMultipleComponent]
public class OceanRespawn : MonoBehaviour
{
    [Header("Laut")]
    [Tooltip("Permukaan laut. Player di bawah tinggi ini dianggap tenggelam.")]
    public float seaLevel = -0.25f;

    [Tooltip("Tambahan toleransi sebelum teleport (detik).")]
    public float respawnDelay = 0.15f;

    [Header("Spawn")]
    [Tooltip("Kosongkan untuk memakai posisi player saat script pertama kali jalan.")]
    public Transform spawnPoint;

    public Vector3 SpawnPosition { get; private set; }
    public Quaternion SpawnRotation { get; private set; }

    float underwaterTime;
    bool teleporting;

    void Start()
    {
        if (spawnPoint != null)
        {
            SpawnPosition = spawnPoint.position;
            SpawnRotation = spawnPoint.rotation;
        }
        else
        {
            SpawnPosition = transform.position;
            SpawnRotation = transform.rotation;
        }
    }

    void Update()
    {
        if (teleporting) return;

        if (transform.position.y >= seaLevel)
        {
            underwaterTime = 0f;
            return;
        }

        underwaterTime += Time.deltaTime;
        if (underwaterTime < Mathf.Max(0f, respawnDelay)) return;

        teleporting = true;
        ResetMotion();

        // SimplePlayerController punya Teleport() yang menonaktifkan CharacterController
        // saat memindahkan posisi. Memakai transform.position biasa akan dikembalikan
        // oleh controller ke posisi lama pada frame berikutnya.
        ITeleportable teleportable = GetComponent<ITeleportable>();
        if (teleportable != null)
            teleportable.Teleport(SpawnPosition, SpawnRotation);
        else
            transform.SetPositionAndRotation(SpawnPosition, SpawnRotation);

        underwaterTime = 0f;

        if (TreeEditController.Instance != null)
            TreeEditController.Instance.ShowHint("Kamu jatuh ke laut. Kembali ke titik awal.", 2.5f);

        teleporting = false;
    }

    void ResetMotion()
    {
        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null) body = GetComponentInChildren<Rigidbody>();
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }
}
