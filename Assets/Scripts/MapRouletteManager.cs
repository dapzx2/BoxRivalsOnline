using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections;
using TMPro;

[RequireComponent(typeof(PhotonView))]
public class MapRouletteManager : MonoBehaviourPunCallbacks
{
    public string[] mapSceneNames = { "Level3_SkyPlatforms", "Level3_ObstacleRush", "Level3_RampRace" };
    public string[] mapDisplayNames = { "Sky Platforms", "Obstacle Rush", "Ramp Race" };
    public Image[] mapCards;
    public TextMeshProUGUI selectionText;
    public GameObject particleEffect;
    public float shuffleDuration = 3f;
    public float initialSpeed = 0.05f;
    public float finalSpeed = 0.3f;

    private int selectedMapIndex = -1;
    private bool isAnimating;

    void Start()
    {
        if (particleEffect != null) particleEffect.SetActive(false);
        if (PhotonNetwork.IsMasterClient)
        {
            selectedMapIndex = 0;
            photonView.RPC(nameof(RpcStartRoulette), RpcTarget.AllBuffered, selectedMapIndex);
        }
    }

    [PunRPC]
    void RpcStartRoulette(int mapIndex)
    {
        selectedMapIndex = mapIndex;
        if (!isAnimating) StartCoroutine(PlayRouletteAnimation());
    }

    IEnumerator PlayRouletteAnimation()
    {
        isAnimating = true;
        if (selectionText != null) selectionText.text = "Memilih Map...";

        float elapsed = 0f;
        int currentHighlight = 0;
        float currentDelay = initialSpeed;

        while (elapsed < shuffleDuration)
        {
            HighlightCard(currentHighlight);
            yield return new WaitForSeconds(currentDelay);
            currentHighlight = (currentHighlight + 1) % mapCards.Length;
            elapsed += currentDelay;
            float progress = elapsed / shuffleDuration;
            currentDelay = Mathf.Lerp(initialSpeed, finalSpeed, progress * progress);
        }

        HighlightCard(selectedMapIndex, true);
        if (selectionText != null) selectionText.text = mapDisplayNames[selectedMapIndex] + "!";
        if (particleEffect != null) particleEffect.SetActive(true);

        yield return new WaitForSeconds(2f);

        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel(mapSceneNames[selectedMapIndex]);
    }

    void HighlightCard(int index, bool isFinal = false)
    {
        for (int i = 0; i < mapCards.Length; i++)
        {
            if (mapCards[i] == null) continue;
            float scale = (i == index) ? (isFinal ? 1.2f : 1.1f) : 0.9f;
            mapCards[i].transform.localScale = Vector3.one * scale;
            mapCards[i].color = (i == index) ? (isFinal ? Color.green : Color.yellow) : Color.white;
        }
    }
}
