using System;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem; // New Input System (project memakai Active Input Handling = Input System)

/// <summary>
/// Mengatur kamera aktif: Isometric (Ortho Camera) atau First-Person (Perspective Camera).
/// Prioritas CinemachineCamera yang dipakai, jadi transisi tetap diblend oleh CinemachineBrain.
/// Keyboard V tetap berfungsi sebagai fallback di Editor; di mobile tombol UI memanggil ToggleMode().
/// </summary>
public class CameraSwitcher : MonoBehaviour
{
    public enum Mode
    {
        Isometric = 0,
        FirstPerson = 1
    }

    [Header("Cameras")]
    [Tooltip("CinemachineCamera untuk mode first-person / shoulder cam")]
    public CinemachineCamera perspectiveCam;

    [Tooltip("CinemachineCamera untuk mode isometric")]
    public CinemachineCamera orthoCam;

    [Header("Priorities")]
    [Range(0, 100)] public int activePriority = 10;
    [Range(0, 100)] public int inactivePriority = 0;

    [Header("Keyboard (Editor)")]
    public bool keyboardShortcut = true;
    public Key toggleKey = Key.V;

    /// <summary>Mode kamera yang sedang aktif.</summary>
    public Mode CurrentMode { get; private set; } = Mode.Isometric;

    /// <summary>Dipanggil setiap kali mode berubah (dipakai UI untuk update label).</summary>
    public event Action<Mode> ModeChanged;

    void Awake()
    {
        // Mulai dari prioritas yang sudah di-set di scene supaya tidak "loncat" saat play.
        if (perspectiveCam != null && orthoCam != null)
        {
            CurrentMode = perspectiveCam.Priority > orthoCam.Priority ? Mode.FirstPerson : Mode.Isometric;
        }

        // Pastikan prioritas deterministik (di scene keduanya 0, jadi Cinemachine bisa salah pilih).
        ApplyPriority();
    }

    void Update()
    {
        if (!keyboardShortcut) return;
        if (Keyboard.current == null) return;
        if (Keyboard.current[toggleKey].wasPressedThisFrame) ToggleMode();
    }

    public void ToggleMode()
    {
        SetMode(CurrentMode == Mode.Isometric ? Mode.FirstPerson : Mode.Isometric);
    }

    public void SetMode(Mode mode)
    {
        if (mode == CurrentMode) return;
        CurrentMode = mode;
        ApplyPriority();
        ModeChanged?.Invoke(CurrentMode);
    }

    void ApplyPriority()
    {
        if (perspectiveCam == null || orthoCam == null) return;

        bool firstPerson = CurrentMode == Mode.FirstPerson;
        perspectiveCam.Priority = firstPerson ? activePriority : inactivePriority;
        orthoCam.Priority = firstPerson ? inactivePriority : activePriority;
    }
}
