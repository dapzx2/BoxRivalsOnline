using UnityEngine;

public class CameraActivationManager : MonoBehaviour
{
    public CameraController camera1Controller;
    public CameraController camera2Controller;

    [SerializeField] private UIManager uiManager;

    void Start()
    {
        // Pastikan UIManager sudah di-assign di Inspector
        if (uiManager == null)
        {
            Debug.LogError("UIManager belum di-assign di Inspector untuk CameraActivationManager!");
        }
        
        // Tetap atur default di sini untuk awal
        if (camera1Controller != null && camera2Controller != null)
        {
            SwitchToCamera1();
        }
    }

    void Update()
    {
        if (camera1Controller == null || camera2Controller == null) return;

        // Cek klik mouse
        if (Input.GetMouseButtonDown(0))
        {
            // Pastikan game tidak sedang dijeda oleh panel cerita awal
            if (uiManager != null && uiManager.panelCeritaAwal.activeInHierarchy)
            {
                return; // Jangan lakukan apa-apa jika panel cerita masih aktif
            }

            float mouseXPosition = Input.mousePosition.x;
            float screenHalfWidth = Screen.width / 2f;

            if (mouseXPosition < screenHalfWidth) { SwitchToCamera1(); }
            else { SwitchToCamera2(); }
        }

        // Cek input keyboard
        if (camera1Controller.isControlActive)
        {
            if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.L))
            {
                SwitchToCamera2();
            }
        }
        else if (camera2Controller.isControlActive)
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D))
            {
                SwitchToCamera1();
            }
        }
    }

    // --- FUNGSI INI AKAN KITA BUAT PUBLIK AGAR BISA DIPANGGIL DARI LUAR ---
    public void SwitchToCamera1()
    {
        if (camera1Controller == null || camera2Controller == null) return;
        camera1Controller.isControlActive = true;
        camera2Controller.isControlActive = false;
    }

    public void SwitchToCamera2()
    {
        if (camera1Controller == null || camera2Controller == null) return;
        camera1Controller.isControlActive = false;
        camera2Controller.isControlActive = true;
    }
}