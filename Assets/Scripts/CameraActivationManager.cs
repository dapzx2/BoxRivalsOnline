using UnityEngine;

/// <summary>
/// Manager untuk mengaktifkan kamera berdasarkan klik mouse
/// Untuk multiplayer online, setiap client hanya punya 1 kamera aktif
/// </summary>
public class CameraActivationManager : MonoBehaviour
{
    public CameraController camera1Controller;
    public CameraController camera2Controller;

    [SerializeField] private UIManager uiManager;

    void Start()
    {
        // Default: aktifkan kamera 1
        if (camera1Controller != null)
        {
            camera1Controller.isControlActive = true;
        }
        if (camera2Controller != null)
        {
            camera2Controller.isControlActive = false;
        }
    }

    void Update()
    {
        if (camera1Controller == null || camera2Controller == null) return;

        // Switch kamera berdasarkan klik mouse (kiri/kanan layar)
        if (Input.GetMouseButtonDown(0))
        {
            if (uiManager != null && uiManager.panelCeritaAwal != null && uiManager.panelCeritaAwal.activeInHierarchy)
            {
                return;
            }

            float mouseXPosition = Input.mousePosition.x;
            float screenHalfWidth = Screen.width / 2f;

            if (mouseXPosition < screenHalfWidth)
            {
                camera1Controller.isControlActive = true;
                camera2Controller.isControlActive = false;
            }
            else
            {
                camera1Controller.isControlActive = false;
                camera2Controller.isControlActive = true;
            }
        }
    }
}
