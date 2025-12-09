using UnityEngine;

public class CameraActivationManager : MonoBehaviour
{
    public CameraController camera1Controller;
    public CameraController camera2Controller;
    [SerializeField] private UIManager uiManager;

    void Start()
    {
        if (camera1Controller != null) camera1Controller.isControlActive = true;
        if (camera2Controller != null) camera2Controller.isControlActive = false;
    }

    void Update()
    {
        if (camera1Controller == null || camera2Controller == null) return;
        if (!Input.GetMouseButtonDown(0)) return;
        if (uiManager?.panelCeritaAwal != null && uiManager.panelCeritaAwal.activeInHierarchy) return;

        bool leftSide = Input.mousePosition.x < Screen.width / 2f;
        camera1Controller.isControlActive = leftSide;
        camera2Controller.isControlActive = !leftSide;
    }
}
