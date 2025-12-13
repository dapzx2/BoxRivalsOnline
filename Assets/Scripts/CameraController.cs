using UnityEngine;
using UnityEngine.SceneManagement;

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
    private bool hasInitialized;

    void OnEnable()
    {
        hasInitialized = false;
        
        if (GetComponent<AudioListener>() == null)
            gameObject.AddComponent<AudioListener>();
            
        AudioListener.pause = false;
        AudioListener.volume = 1f;
    }

    void LateUpdate()
    {
        if (!currentTarget) return;

        if (!hasInitialized)
        {
            InitializeCameraRotation();
            hasInitialized = true;
        }

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

    void InitializeCameraRotation()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        
        if (sceneName == SceneNames.Level3_RampRace || 
            sceneName == SceneNames.Level3_ObstacleRush || 
            sceneName == SceneNames.Level3_SkyPlatforms)
        {
            rotasiY = 90f;
            rotasiX = 25f;
        }
        else
        {
            rotasiY = currentTarget.eulerAngles.y;
        }
    }
}