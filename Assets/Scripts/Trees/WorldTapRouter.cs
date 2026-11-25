using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Menerjemahkan tap singkat di area swipe menjadi aksi dunia.
/// Prioritas: toko -> dekomposer -> kolam (isi ulang penyiram) -> rawat pohon
/// -> edit mode / ambil mayat hama.
/// Saat edit mode aktif, panel rawat pohon dilewati: tap hanya untuk menanam
/// dan menghapus pohon.
/// Tap dibedakan dari swipe lewat jarak &amp; durasi, jadi menggeser kamera tidak
/// akan memicu aksi.
/// </summary>
[DisallowMultipleComponent]
public class WorldTapRouter : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Tooltip("Jarak maksimum jari bergeser (pixel) supaya masih dihitung tap.")]
    public float maxTapDistance = 28f;

    [Tooltip("Durasi maksimum tap (detik).")]
    public float maxTapDuration = 0.45f;

    CameraSwitcher switcher;
    Transform player;
    int pointerId = -1;
    Vector2 startPosition;
    float startTime;

    void Awake()
    {
        switcher = FindFirstObjectByType<CameraSwitcher>();
        ResolvePlayer();
    }

    void ResolvePlayer()
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null) player = playerObject.transform;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (pointerId >= 0) return;

        pointerId = eventData.pointerId;
        startPosition = eventData.position;
        startTime = Time.unscaledTime;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != pointerId) return;
        pointerId = -1;

        if ((eventData.position - startPosition).magnitude > maxTapDistance) return;
        if (Time.unscaledTime - startTime > maxTapDuration) return;

        // Panel yang sedang terbuka sudah punya blocker sendiri.
        if (ShopPanel.AnyOpen || DecomposerPanel.AnyOpen || TreeCarePanel.AnyOpen) return;

        if (switcher == null) switcher = FindFirstObjectByType<CameraSwitcher>();
        if (switcher == null) return;

        if (player == null) ResolvePlayer();

        Camera camera = Camera.main;
        Vector2 position = eventData.position;

        if (ShopStation.TryHandleTapFromScreen(position, camera, player)) return;
        if (DecomposerStation.TryHandleTapFromScreen(position, camera, player)) return;

        // Kolam: tap untuk mengisi ulang air Penyiram.
        if (PondRefillStation.TryHandleTapFromScreen(position, camera, player)) return;

        // [Isometric] Edit mode: rawat pohon dimatikan, tap langsung ke tanam/hapus.
        bool editing = TreeEditController.Instance != null && TreeEditController.Instance.IsEditing;
        if (!editing && TreeCareController.TryHandleTapFromScreen(position)) return;

        if (switcher.CurrentMode == CameraSwitcher.Mode.Isometric)
        {
            if (TreeEditController.Instance != null)
                TreeEditController.Instance.TryHandleTapFromScreen(position);
        }
        else
        {
            PestPickupController.TryPickupFromScreen(position, camera, switcher);
        }
    }
}
