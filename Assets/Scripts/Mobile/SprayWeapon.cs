using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [FPS] Viewmodel dan umpan balik visual untuk senjata semprotan.
/// Dipasang di kamera first-person. Model semprotan harus menjadi child kamera
/// (bukan child Player, karena renderer Player dimatikan saat mode FPS).
/// Efek semburan dipancarkan dari firepoint lewat event MobileAttackInput.onAttack.
/// </summary>
[DisallowMultipleComponent]
public class SprayWeapon : MonoBehaviour
{
    [Header("References")]
    public CameraSwitcher switcher;
    public GameObject viewModel;
    public ParticleSystem sprayEffect;
    public Transform muzzle;
    public GameObject crosshair;

    [Header("Reload UI")]
    public MobileAttackInput attackInput;
    public GameObject reloadBarRoot;
    public Image reloadFill;

    [Header("Pose")]
    public Vector3 restPosition = new Vector3(0.28f, -0.34f, 0.55f);
    public Vector3 restEuler = new Vector3(-12f, 72f, 4f);

    [Header("Recoil & Sway")]
    public float recoilKick = 0.055f;
    public float recoilRecovery = 9f;
    public float swayAmount = 0.004f;
    public float swaySpeed = 1.7f;

    Vector3 poseBase;
    float recoil;

    void Awake()
    {
        if (switcher == null) switcher = FindFirstObjectByType<CameraSwitcher>();
        if (attackInput == null) attackInput = FindFirstObjectByType<MobileAttackInput>();
        if (viewModel != null)
        {
            viewModel.transform.localPosition = restPosition;
            viewModel.transform.localEulerAngles = restEuler;
            poseBase = restPosition;
        }
    }

    void Update()
    {
        if (viewModel == null) return;

        bool fps = switcher == null || switcher.CurrentMode == CameraSwitcher.Mode.FirstPerson;
        if (viewModel.activeSelf != fps) viewModel.SetActive(fps);
        if (crosshair != null && crosshair.activeSelf != fps) crosshair.SetActive(fps);
        if (reloadBarRoot != null && reloadBarRoot.activeSelf != fps) reloadBarRoot.SetActive(fps);
        if (!fps) return;

        bool reloading = attackInput != null && attackInput.IsReloading;
        if (reloadBarRoot != null && reloadBarRoot.activeSelf != reloading)
            reloadBarRoot.SetActive(reloading);
        if (reloadFill != null)
            reloadFill.fillAmount = attackInput != null ? attackInput.ReloadProgress : 0f;

        recoil = Mathf.MoveTowards(recoil, 0f, Mathf.Max(0.01f, recoilRecovery) * Time.deltaTime);

        float sway = Mathf.Sin(Time.time * swaySpeed) * swayAmount;
        float swayY = Mathf.Cos(Time.time * swaySpeed * 1.3f) * swayAmount;

        viewModel.transform.localPosition = poseBase +
            new Vector3(sway, swayY - recoil * 0.25f, -recoil);
        viewModel.transform.localEulerAngles = restEuler +
            new Vector3(recoil * 22f, 0f, 0f);
    }

    /// <summary>Dipanggil lewat MobileAttackInput.onAttack setiap kali menyemprot.</summary>
    public void OnFire()
    {
        recoil = Mathf.Min(recoilKick * 1.8f, recoil + recoilKick);

        if (sprayEffect == null) return;

        if (muzzle != null)
        {
            sprayEffect.transform.SetPositionAndRotation(muzzle.position, muzzle.rotation);
        }

        sprayEffect.Emit(10);
    }
}
