using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform currentTarget;
    public bool isControlActive;

    [Header("Camera")]
    public float jarak = 5f;
    public float kecepatanPutar = 2f;
    public float batasAtas = 80f;
    public float batasBawah = -40f;

    [Header("Zoom")]
    public float kecepatanZoom = 10f;
    public float jarakMinimal = 2f;
    public float jarakMaksimal = 15f;

    private float rotasiX = 20f;
    private float rotasiY;

    void Start() { if (currentTarget != null) rotasiY = currentTarget.eulerAngles.y; }

    void LateUpdate()
    {
        if (!currentTarget) return;

        if (isControlActive)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            jarak = Mathf.Clamp(jarak - scroll * kecepatanZoom, jarakMinimal, jarakMaksimal);

            if (Input.GetMouseButton(1))
            {
                rotasiY += Input.GetAxis("Mouse X") * kecepatanPutar;
                rotasiX -= Input.GetAxis("Mouse Y") * kecepatanPutar;
                rotasiX = Mathf.Clamp(rotasiX, batasBawah, batasAtas);
            }
        }

        Quaternion rotasi = Quaternion.Euler(rotasiX, rotasiY, 0);
        transform.rotation = rotasi;
        transform.position = rotasi * new Vector3(0, 0, -jarak) + currentTarget.position;
    }
}