using UnityEngine;

/// <summary>
/// Peluru mode FPS. Bergerak lurus dari FirePoint memakai SphereCast supaya tidak
/// menembus collider tipis, lalu hancur saat kena sesuatu atau umurnya habis.
/// Kalau objek yang kena punya <see cref="IDamageable"/>, peluru memberi damage.
/// </summary>
[DisallowMultipleComponent]
public class MobileBullet : MonoBehaviour
{
    [Header("Motion")]
    public float speed = 60f;
    public float lifetime = 3f;
    [Tooltip("Jari-jari peluru untuk SphereCast (meter).")]
    public float radius = 0.06f;

    [Header("Damage")]
    public float damage = 20f;

    [Header("Collision")]
    public LayerMask hitMask = ~0;

    [Tooltip("Root penembak; collider miliknya diabaikan.")]
    public Transform ignoreRoot;

    [Header("Events")]
    public BulletHitEvent onHit;

    float dieAt;
    bool hitDone;

    [System.Serializable]
    public class BulletHitEvent : UnityEngine.Events.UnityEvent<RaycastHit> { }

    void Awake()
    {
        dieAt = Time.time + lifetime;
    }

    /// <summary>Dipanggil penembak tepat setelah Instantiate.</summary>
    public void Launch(Vector3 direction, Transform shooter, float bulletSpeed, float bulletLifetime, LayerMask mask)
    {
        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        ignoreRoot = shooter;
        speed = bulletSpeed;
        lifetime = bulletLifetime;
        hitMask = mask;
        dieAt = Time.time + Mathf.Max(0.05f, bulletLifetime);
    }

    void Update()
    {
        if (hitDone) return;

        float step = speed * Time.deltaTime;
        if (step <= 0f) return;

        if (Physics.SphereCast(transform.position, radius, transform.forward, out RaycastHit hit,
                step + radius, hitMask, QueryTriggerInteraction.Ignore))
        {
            if (!IsShooter(hit.collider))
            {
                OnHit(hit);
                return;
            }
        }

        transform.position += transform.forward * step;
        if (Time.time >= dieAt) Destroy(gameObject);
    }

    bool IsShooter(Collider other)
    {
        if (ignoreRoot == null || other == null) return false;
        Transform t = other.transform;
        return t == ignoreRoot || t.IsChildOf(ignoreRoot);
    }

    void OnHit(RaycastHit hit)
    {
        hitDone = true;

        IDamageable damageable = hit.collider != null ? hit.collider.GetComponentInParent<IDamageable>() : null;
        if (damageable != null) damageable.ApplyDamage(damage, hit.point);

        onHit?.Invoke(hit);
        Destroy(gameObject);
    }
}
