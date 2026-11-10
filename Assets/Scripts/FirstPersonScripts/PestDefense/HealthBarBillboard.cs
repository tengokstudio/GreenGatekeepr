using UnityEngine;

/// <summary>
/// Membuat health bar 3D selalu menghadap kamera (tetap tegak).
/// Dipasang di root health bar tiap pohon.
/// </summary>
[DisallowMultipleComponent]
public class HealthBarBillboard : MonoBehaviour
{
    public Camera targetCamera;

    void LateUpdate()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;

        Vector3 direction = transform.position - targetCamera.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }
}
