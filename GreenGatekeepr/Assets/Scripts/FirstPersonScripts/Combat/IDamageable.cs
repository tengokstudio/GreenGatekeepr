using UnityEngine;

/// <summary>
/// Kontrak sederhana untuk semua objek yang bisa menerima damage
/// (pohon, hama, nanti bisa player/enemy lain).
/// </summary>
public interface IDamageable
{
    void ApplyDamage(float amount, Vector3 hitPoint);
}
