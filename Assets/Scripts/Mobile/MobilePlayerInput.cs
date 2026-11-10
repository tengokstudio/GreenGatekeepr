using Unity.Cinemachine.Samples;
using UnityEngine;

/// <summary>
/// Menyambungkan virtual joystick ke SimplePlayerController bawaan sample Cinemachine.
/// Nilai joystick ditulis ke InputAxis MoveX / MoveZ, jadi fisika, animasi, dan
/// rotasi karakter tetap memakai logika SimplePlayerController (CharacterController).
/// </summary>
[DisallowMultipleComponent]
public class MobilePlayerInput : MonoBehaviour
{
    [Header("References")]
    public VirtualJoystick joystick;

    [Header("Movement")]
    [Tooltip("Kecepatan gerak default. Karakter langsung lari, bukan jalan pelan.")]
    public float runSpeed = 8f;

    [Tooltip("Timpa Speed bawaan sample (2.5 = jalan pelan) saat play.")]
    public bool applyRunSpeed = true;

    [Header("Tuning")]
    [Tooltip("Batas bawah input yang diabaikan.")]
    [Range(0f, 0.4f)] public float deadZone = 0.1f;

    SimplePlayerController controller;

    void Awake()
    {
        controller = GetComponent<SimplePlayerController>();
        if (controller == null) controller = GetComponentInChildren<SimplePlayerController>(true);

        if (applyRunSpeed && controller != null && runSpeed > 0f)
            controller.Speed = runSpeed;
    }

    void Update()
    {
        Vector2 value = joystick != null ? joystick.Value : Vector2.zero;
        if (value.magnitude < deadZone) value = Vector2.zero;

        if (controller == null) return;
        controller.MoveX.Value = value.x;
        controller.MoveZ.Value = value.y;
    }

    void OnDisable()
    {
        if (controller == null) return;
        controller.MoveX.Value = 0f;
        controller.MoveZ.Value = 0f;
    }
}
