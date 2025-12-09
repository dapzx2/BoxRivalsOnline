using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using System.Collections;

/// <summary>
/// Manages the map selection roulette animation for Level 3
/// Similar to Stumble Guys map selection
/// </summary>
public class MapRouletteManager : MonoBehaviourPunCallbacks
{
    [Header("Map Variants")]
    [SerializeField] private string[] mapSceneNames = { "Level3_SkyPlatforms", "Level3_ObstacleRush", "Level3_RampRace" };
    [SerializeField] private string[] mapDisplayNames = { "Sky Platforms", "Obstacle Rush", "Ramp Race" };
    
    [Header("UI References")]
    [SerializeField] private GameObject[] mapCards; // 3 UI cards for each map
    [SerializeField] private Text selectionText;
    [SerializeField] private GameObject particleEffect;
    
    [Header("Animation Settings")]
    [SerializeField] private float shuffleDuration = 2.5f;
    [SerializeField] private float shuffleSpeed = 10f;
    [SerializeField] private AnimationCurve slowdownCurve;

    private int selectedMapIndex = -1;

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // Master client picks random map
            selectedMapIndex = Random.Range(0, mapSceneNames.Length);
            
            // Sync to all clients via RPC
            photonView.RPC("RpcSetSelectedMap", RpcTarget.AllBuffered, selectedMapIndex);
        }
    }

    [PunRPC]
    void RpcSetSelectedMap(int mapIndex)
    {
        selectedMapIndex = mapIndex;
        StartCoroutine(PlayRouletteAnimation());
    }

    IEnumerator PlayRouletteAnimation()
    {
        selectionText.text = "Randomizing Map...";

        float elapsed = 0f;
        int currentHighlight = 0;

        // Shuffle animation
        while (elapsed < shuffleDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shuffleDuration;
            
            // Speed decreases over time using curve
            float currentSpeed = shuffleSpeed * (1f - slowdownCurve.Evaluate(t));
            
            // Highlight cards in sequence
            if (currentSpeed > 0.5f)
            {
                currentHighlight = (currentHighlight + 1) % mapCards.Length;
                HighlightCard(currentHighlight);
            }

            yield return new WaitForSeconds(0.1f);
        }

        // Final selection
        HighlightCard(selectedMapIndex, true);
        selectionText.text = mapDisplayNames[selectedMapIndex] + "!";
        
        // Particle effect
        if (particleEffect != null)
        {
            particleEffect.SetActive(true);
        }

        // Wait before loading scene
        yield return new WaitForSeconds(2f);

        // Load selected map scene
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(mapSceneNames[selectedMapIndex]);
        }
    }

    private void HighlightCard(int index, bool isFinal = false)
    {
        for (int i = 0; i < mapCards.Length; i++)
        {
            if (mapCards[i] != null)
            {
                // Scale animation
                float scale = (i == index) ? (isFinal ? 1.3f : 1.1f) : 1f;
                mapCards[i].transform.localScale = Vector3.one * scale;

                // Color/glow effect
                Image cardImage = mapCards[i].GetComponent<Image>();
                if (cardImage != null)
                {
                    cardImage.color = (i == index) ? Color.yellow : Color.white;
                }
            }
        }
    }
}
