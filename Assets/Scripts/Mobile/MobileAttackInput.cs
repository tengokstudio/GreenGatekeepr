using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Tombol attack untuk mode first-person.
/// Tombol hanya tampil saat mode FPS. Saat ditekan, peluru di-instantiate dari
/// FirePoint (child kamera FPS) sehingga arah tembakan mengikuti pandangan.
/// onAttack tetap tersedia sebagai hook tambahan (misal suara / animasi).
/// </summary>
[DisallowMultipleComponent]
public class MobileAttackInput : MonoBehaviour
{
    [Header("References")]
    public CameraSwitcher switcher;

    [Tooltip("Root tombol attack yang ditampilkan/disembunyikan otomatis.")]
    public GameObject buttonRoot;

    [Tooltip("Titik munculnya peluru. Biasanya child dari kamera FPS.")]
    public Transform firePoint;

    [Tooltip("Prefab peluru. Kalau kosong, dibuat bola sederhana saat runtime.")]
    public GameObject bulletPrefab;

    public Animator animator;

    [Header("Attack")]
    [Tooltip("Nama trigger Animator. Kosongkan kalau tidak dipakai (biar tidak ada warning).")]
    public string animatorTrigger = "";

    [Tooltip("Jeda minimum antar tembakan (detik).")]
    [Range(0f, 2f)] public float cooldown = 0.25f;

    [Header("Bullet")]
    public float bulletSpeed = 60f;
    public float bulletLifetime = 3f;
    public LayerMask hitMask = ~0;

    [Tooltip("Dipanggil setiap kali tombol attack ditekan (setelah lolos cooldown).")]
    public UnityEvent onAttack;

    float nextAttackTime;
    bool? buttonVisible;

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (switcher == null) switcher = FindFirstObjectByType<CameraSwitcher>();
        RefreshButton();
    }

    void LateUpdate()
    {
        RefreshButton();
    }

    void RefreshButton()
    {
        if (buttonRoot == null || switcher == null) return;

        bool show = switcher.CurrentMode == CameraSwitcher.Mode.FirstPerson;
        if (buttonVisible.HasValue && buttonVisible.Value == show) return;

        buttonVisible = show;
        buttonRoot.SetActive(show);
    }

    /// <summary>Dipanggil oleh Button.onClick.</summary>
    public void Attack()
    {
        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + Mathf.Max(0f, cooldown);

        if (animator != null && !string.IsNullOrEmpty(animatorTrigger))
            animator.SetTrigger(animatorTrigger);

        Fire();

        onAttack?.Invoke();
    }

    void Fire()
    {
        if (firePoint == null) return;

        Vector3 direction = firePoint.forward;
        GameObject bullet = bulletPrefab != null
            ? Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(direction))
            : CreateFallbackBullet();

        if (bullet == null) return;

        MobileBullet projectile = bullet.GetComponent<MobileBullet>();
        if (projectile != null)
            projectile.Launch(direction, transform.root, bulletSpeed, bulletLifetime, hitMask);
    }

    GameObject CreateFallbackBullet()
    {
        GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bullet.name = "Bullet (runtime)";
        bullet.transform.localScale = Vector3.one * 0.12f;

        Collider collider = bullet.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        bullet.transform.SetPositionAndRotation(firePoint.position, Quaternion.LookRotation(firePoint.forward));
        bullet.AddComponent<MobileBullet>();
        return bullet;
    }
}
