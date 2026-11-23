using UnityEngine;

/// <summary>
/// [FPS] Mengambil mayat hama lewat tap di layar.
/// Raycast dari kamera; hanya mayat yang benar-benar terlihat (tidak terhalang)
/// yang bisa diambil. Jarak & syarat lain dicek oleh <see cref="PestEnemy.TryPickup"/>.
/// </summary>
public static class PestPickupController
{
    public static bool TryPickupFromScreen(Vector2 screenPosition, Camera camera, CameraSwitcher switcher)
    {
        if (switcher == null || switcher.CurrentMode != CameraSwitcher.Mode.FirstPerson) return false;

        if (camera == null) camera = Camera.main;
        if (camera == null) return false;

        Ray ray = camera.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 40f, ~0, QueryTriggerInteraction.Ignore);
        if (hits.Length == 0) return false;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // Hit pertama yang terlihat menentukan: kalau bukan mayat hama, tap dianggap meleset.
        PestEnemy pest = hits[0].collider.GetComponentInParent<PestEnemy>();
        if (pest == null || !pest.IsCorpse) return false;

        return pest.TryPickup();
    }
}
