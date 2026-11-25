using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// [FPS] Membuat tombol ATK bisa ditahan untuk menyemprotkan hama terus-menerus.
/// Pasang di GameObject yang sama dengan Button tombol ATK.
/// </summary>
[DisallowMultipleComponent]
public class HoldAttackButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public MobileAttackInput attackInput;

    void Awake()
    {
        if (attackInput == null) attackInput = FindFirstObjectByType<MobileAttackInput>();
    }

    void OnDisable()
    {
        if (attackInput != null) attackInput.StopFire();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (attackInput == null) attackInput = FindFirstObjectByType<MobileAttackInput>();
        if (attackInput != null) attackInput.StartFire();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (attackInput != null) attackInput.StopFire();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (attackInput != null) attackInput.StopFire();
    }
}
