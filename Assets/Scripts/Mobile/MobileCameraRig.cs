using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Rig kamera mobile dengan dua mode:
///  - Isometric : kamera ortho dengan sudut tetap, mengikuti Player, swipe = pan (tanpa rotasi).
///  - FirstPerson: shoulder cam, swipe = putar yaw/pitch.
///
/// Transform kedua CinemachineCamera di-drive script ini (komponen prosedural Cinemachine
/// di-nonaktifkan saat runtime), sehingga CinemachineBrain tetap bisa nge-blend antar mode.
/// </summary>
[DisallowMultipleComponent]
public class MobileCameraRig : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Camera mainCamera;
    public CinemachineCamera isoCam;
    public CinemachineCamera fpCam;
    public CameraSwitcher switcher;

    [Tooltip("Label kecil di tombol mode (opsional).")]
    public Text modeLabel;

    [Header("Isometric")]
    public float isoYaw = 45f;
    public float isoPitch = 45f;
    public float isoDistance = 26f;
    public float isoOrthoSize = 9f;
    public float isoPanRadius = 10f;
    [Tooltip("Seberapa cepat kamera menyusul Player / target pan.")]
    public float isoFollowDamping = 8f;

    [Header("First Person (shoulder cam)")]
    public Vector3 shoulderOffset = new Vector3(0.35f, 1.45f, 0.15f);
    public float fpFollowDamping = 16f;
    [Tooltip("Derajat per pixel swipe.")]
    public float lookSensitivity = 0.18f;
    public Vector2 pitchLimits = new Vector2(-60f, 75f);
    public float fpFieldOfView = 62f;

    [Header("Mobile runtime")]
    public bool disableSampleCameraInput = true;
    public bool applyMobileFrameRate = true;
    [Range(30, 120)] public int targetFrameRate = 60;

    Vector3 isoFocus;
    Vector3 panOffset;
    Vector3 fpSmoothedPosition;
    float yaw;
    float pitch = 10f;
    CameraSwitcher.Mode lastMode;
    bool modeKnown;
    bool? labelShowsIsometric;

    void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (switcher == null) switcher = GetComponent<CameraSwitcher>();

        if (player == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null) player = playerObject.transform;
        }

        if (disableSampleCameraInput) DisableSampleCameraControls();

        if (applyMobileFrameRate)
        {
            Application.targetFrameRate = targetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        lastMode = switcher != null ? switcher.CurrentMode : CameraSwitcher.Mode.Isometric;
        modeKnown = true;

        yaw = player != null ? player.eulerAngles.y : 0f;
        isoFocus = player != null ? player.position : Vector3.zero;
        fpSmoothedPosition = player != null ? player.position + shoulderOffset : Vector3.zero;

        UpdateLabel();
    }

    void LateUpdate()
    {
        if (player == null || isoCam == null || fpCam == null) return;

        // Deteksi perubahan mode tanpa bergantung pada event (lebih tahan terhadap urutan Awake).
        if (switcher != null)
        {
            CameraSwitcher.Mode current = switcher.CurrentMode;
            if (!modeKnown || current != lastMode)
            {
                lastMode = current;
                modeKnown = true;
                HandleModeChanged(current);
            }
        }

        if (IsIsometric) UpdateIsometric();
        else UpdateFirstPerson();

        UpdateLabel();
    }

    bool IsIsometric => switcher == null || switcher.CurrentMode == CameraSwitcher.Mode.Isometric;

    void UpdateIsometric()
    {
        Vector3 targetFocus = player.position + panOffset;
        float t = 1f - Mathf.Exp(-isoFollowDamping * Time.deltaTime);
        isoFocus = Vector3.Lerp(isoFocus, targetFocus, t);

        Quaternion rotation = Quaternion.Euler(isoPitch, isoYaw, 0f);
        Vector3 position = isoFocus - rotation * Vector3.forward * isoDistance;

        isoCam.transform.SetPositionAndRotation(position, rotation);
        isoCam.Lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
        isoCam.Lens.OrthographicSize = isoOrthoSize;
    }

    void UpdateFirstPerson()
    {
        Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);
        Vector3 desired = player.position + yawRotation * shoulderOffset;

        // Posisi sedikit di-damp supaya tidak "jitter" saat karakter berjalan,
        // rotasi tetap instan supaya aiming terasa responsif.
        float t = 1f - Mathf.Exp(-fpFollowDamping * Time.deltaTime);
        fpSmoothedPosition = Vector3.Lerp(fpSmoothedPosition, desired, t);

        fpCam.transform.SetPositionAndRotation(fpSmoothedPosition, Quaternion.Euler(pitch, yaw, 0f));
        fpCam.Lens.ModeOverride = LensSettings.OverrideModes.Perspective;
        fpCam.Lens.FieldOfView = fpFieldOfView;
    }

    // ------------------------------------------------------------------
    // Dipanggil oleh TouchSwipeArea
    // ------------------------------------------------------------------
    public void OnSwipeStart()
    {
        // Hook kalau nanti perlu efek visual saat swipe mulai (misal fade UI).
    }

    public void OnSwipeEnd()
    {
        // Hook kalau nanti perlu efek visual saat swipe selesai.
    }

    public void OnSwipe(Vector2 deltaPixels)
    {
        if (IsIsometric)
        {
            // Pan: geser titik fokus kamera di bidang XZ, tanpa rotasi.
            float worldPerPixel = (isoOrthoSize * 2f) / Mathf.Max(1, Screen.height);
            Quaternion flatRotation = Quaternion.Euler(0f, isoYaw, 0f);
            Vector3 move = (flatRotation * Vector3.right * deltaPixels.x
                            + flatRotation * Vector3.forward * deltaPixels.y) * worldPerPixel;
            panOffset = Vector3.ClampMagnitude(panOffset + move, isoPanRadius);
        }
        else
        {
            yaw += deltaPixels.x * lookSensitivity;
            pitch = Mathf.Clamp(pitch - deltaPixels.y * lookSensitivity, pitchLimits.x, pitchLimits.y);
        }
    }

    // ------------------------------------------------------------------
    // Dipanggil oleh tombol UI
    // ------------------------------------------------------------------
    public void ToggleCameraMode()
    {
        if (switcher != null) switcher.ToggleMode();
    }

    void HandleModeChanged(CameraSwitcher.Mode mode)
    {
        if (mode == CameraSwitcher.Mode.FirstPerson && player != null)
        {
            // Mulai dari arah hadap karakter supaya kamera tidak "loncat".
            yaw = player.eulerAngles.y;
            pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);
            fpSmoothedPosition = player.position + Quaternion.Euler(0f, yaw, 0f) * shoulderOffset;
        }
        else
        {
            panOffset = Vector3.zero;
            if (player != null) isoFocus = player.position;
        }

        UpdateLabel();
    }

    void UpdateLabel()
    {
        if (modeLabel == null) return;

        bool isometric = IsIsometric;
        if (labelShowsIsometric.HasValue && labelShowsIsometric.Value == isometric) return;

        labelShowsIsometric = isometric;
        modeLabel.text = isometric ? "ISO" : "FPS";
    }

    void DisableSampleCameraControls()
    {
        // Input kamera bawaan sample (mouse/keyboard) dimatikan supaya tidak berebut dengan swipe.
        if (player != null)
        {
            CinemachineInputAxisController input = player.GetComponent<CinemachineInputAxisController>();
            if (input != null) input.enabled = false;
        }

        // Komponen prosedural di vcam dimatikan supaya transform vcam sepenuhnya milik script ini.
        if (fpCam != null)
        {
            DisableAll<CinemachineFollow>(fpCam.gameObject);
            DisableAll<CinemachineRotationComposer>(fpCam.gameObject);
        }

        if (isoCam != null)
        {
            DisableAll<CinemachinePositionComposer>(isoCam.gameObject);
        }
    }

    static void DisableAll<T>(GameObject target) where T : MonoBehaviour
    {
        T[] components = target.GetComponents<T>();
        for (int i = 0; i < components.Length; i++) components[i].enabled = false;
    }
}
