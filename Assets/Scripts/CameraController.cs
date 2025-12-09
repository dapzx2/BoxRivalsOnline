using UnityEngine;

public class CameraController : MonoBehaviour
{
    // Target yang akan diikuti (diisi otomatis oleh script Player)
    public Transform currentTarget; 
    
    // Apakah mouse bisa mengontrol kamera (diisi otomatis oleh script Player)
    public bool isControlActive = false; 

    [Header("Pengaturan Kamera")]
    public float jarak = 5.0f;
    public float kecepatanPutar = 2.0f;
    public float batasAtas = 80.0f;
    public float batasBawah = -40.0f;

    [Header("Pengaturan Zoom")]
    public float kecepatanZoom = 10.0f;
    public float jarakMinimal = 2.0f;
    public float jarakMaksimal = 15.0f;

    private float rotasiX = 20.0f;
    private float rotasiY = 0.0f;

    void Start()
    {
        // Set rotasi awal saat game dimulai (jika target sudah ada)
        if (currentTarget != null)
        {
            rotasiY = currentTarget.eulerAngles.y;
        }
    }

    void LateUpdate()
    {
        // Jika tidak ada target (player belum spawn), jangan lakukan apa-apa
        if (!currentTarget) return; 

        // Hanya proses input mouse jika 'isControlActive' true
        if (isControlActive)
        {
            // Input Zoom (Scroll Wheel)
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            jarak = Mathf.Clamp(jarak - scroll * kecepatanZoom, jarakMinimal, jarakMaksimal);

            // Input Rotasi (Mouse Kanan)
            if (Input.GetMouseButton(1)) // 1 = Tombol mouse kanan
            {
                rotasiY += Input.GetAxis("Mouse X") * kecepatanPutar;
                rotasiX -= Input.GetAxis("Mouse Y") * kecepatanPutar;
                rotasiX = Mathf.Clamp(rotasiX, batasBawah, batasAtas);
            }
        }

        // Hitung rotasi dan posisi kamera
        Quaternion rotasi = Quaternion.Euler(rotasiX, rotasiY, 0);
        Vector3 posisiTarget = currentTarget.position;
        Vector3 posisiKamera = rotasi * new Vector3(0.0f, 0.0f, -jarak) + posisiTarget;

        // Terapkan rotasi dan posisi ke kamera
        transform.rotation = rotasi;
        transform.position = posisiKamera;
    }
}