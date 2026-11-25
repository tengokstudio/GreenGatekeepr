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

    [Tooltip("Saat kapak aktif, tombol ATK dipakai untuk menebang (peluru di-suppress).")]
    public bool suppressBullets;

    [Header("Bullet")]
    public float bulletSpeed = 60f;
    public float bulletLifetime = 3f;
    public LayerMask hitMask = ~0;

    [Tooltip("Dipanggil setiap kali tombol attack ditekan (setelah lolos cooldown).")]
    public UnityEvent onAttack;

    [Tooltip("Aktifkan tembakan terus-menerus saat tombol ATK ditahan (semprotan).")]
    public bool holdToFire = true;

    [Header("Magasin / Reload")]
    [Tooltip("Jumlah tembakan sebelum harus reload.")]
    public int magazineSize = 12;

    [Tooltip("Lama reload dalam detik.")]
    public float reloadDuration = 1.5f;

    public int AmmoRemaining { get; private set; }
    public bool IsReloading { get; private set; }
    public float ReloadProgress => IsReloading
        ? Mathf.Clamp01(reloadTimer / Mathf.Max(0.01f, reloadDuration))
        : 0f;

    float nextAttackTime;
    float reloadTimer;
    bool? buttonVisible;
    bool firing;

    void Awake()
    {
        AmmoRemaining = Mathf.Max(1, magazineSize);
        IsReloading = false;
        reloadTimer = 0f;

        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (switcher == null) switcher = FindFirstObjectByType<CameraSwitcher>();
        RefreshButton();
    }

    void Update()
    {
        if (IsReloading)
        {
            reloadTimer += Time.deltaTime;
            if (reloadTimer >= Mathf.Max(0.01f, reloadDuration))
            {
                IsReloading = false;
                reloadTimer = 0f;
                AmmoRemaining = Mathf.Max(1, magazineSize);
            }
        }

        if (firing && holdToFire && switcher != null &&
            switcher.CurrentMode == CameraSwitcher.Mode.FirstPerson)
            Attack();
    }

    void LateUpdate()
    {
        RefreshButton();
    }

    /// <summary>Dipanggil HoldAttackButton saat tombol ATK ditekan.</summary>
    public void StartFire()
    {
        firing = true;
        Attack();
    }

    /// <summary>Dipanggil HoldAttackButton saat tombol ATK dilepas.</summary>
    public void StopFire()
    {
        firing = false;
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
        if (IsReloading) return;
        if (Time.time < nextAttackTime) return;

        if (AmmoRemaining <= 0)
        {
            StartReload();
            return;
        }

        nextAttackTime = Time.time + Mathf.Max(0f, cooldown);
        AmmoRemaining--;

        if (animator != null && !string.IsNullOrEmpty(animatorTrigger))
            animator.SetTrigger(animatorTrigger);

        if (suppressBullets)
        {
            onAttack?.Invoke();
            if (AmmoRemaining <= 0) StartReload();
            return;
        }

        Fire();

        onAttack?.Invoke();
        if (AmmoRemaining <= 0) StartReload();
    }

    /// <summary>Mulai jeda isi ulang. Dipanggil otomatis saat magasin habis.</summary>
    public void StartReload()
    {
        if (IsReloading) return;
        IsReloading = true;
        reloadTimer = 0f;
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
