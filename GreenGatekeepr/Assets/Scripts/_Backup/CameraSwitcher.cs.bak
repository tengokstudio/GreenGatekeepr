using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem; // Wajib ditambahkan untuk New Input System

public class CameraSwitcher : MonoBehaviour
{
    public CinemachineCamera perspectiveCam;
    public CinemachineCamera orthoCam;

    private bool isPerspectiveActive = true;

    void Update()
    {
        // Mengecek tombol V menggunakan New Input System
        if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
        {
            isPerspectiveActive = !isPerspectiveActive;

            if (isPerspectiveActive)
            {
                perspectiveCam.Priority = 10;
                orthoCam.Priority = 0;
            }
            else
            {
                perspectiveCam.Priority = 0;
                orthoCam.Priority = 10;
            }
        }
    }
}