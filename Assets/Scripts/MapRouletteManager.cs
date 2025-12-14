using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[RequireComponent(typeof(PhotonView))]
public class MapRouletteManager : MonoBehaviourPunCallbacks
{
    public string[] mapSceneNames = { "Level3_SkyPlatforms", "Level3_ObstacleRush", "Level3_RampRace" };
    public string[] mapDisplayNames = { "Sky Platforms", "Obstacle Rush", "Ramp Race" };
    public Image[] mapCards;
    public TextMeshProUGUI selectionText;
    public GameObject particleEffect;
    
    [Header("Animation Settings")]
    public float spinDuration = 3.432f;
    public AudioSource rouletteAudio;
    

    private int selectedMapIndex = -1;
    private bool isAnimating;

    void Start()
    {
        if (particleEffect != null) particleEffect.SetActive(false);
        
        if (rouletteAudio == null) rouletteAudio = gameObject.AddComponent<AudioSource>();
        if (rouletteAudio.clip == null) rouletteAudio.clip = Resources.Load<AudioClip>("Audio/memilih_map");

        if (PhotonNetwork.IsMasterClient)
        {
            RunShuffleBagLogic();
        }
    }

    void RunShuffleBagLogic()
    {
        object availableMapsObj;
        string availableMapsStr = "";
        
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(Constants.AvailableLevel3MapsProperty, out availableMapsObj))
        {
            availableMapsStr = (string)availableMapsObj;
        }

        List<int> availableIndices = new List<int>();
        if (!string.IsNullOrEmpty(availableMapsStr))
        {
            foreach (string s in availableMapsStr.Split(',')) 
            {
                if (int.TryParse(s, out int idx)) availableIndices.Add(idx);
            }
        }

        if (availableIndices.Count == 0)
        {

            for (int i = 0; i < mapSceneNames.Length; i++) availableIndices.Add(i);
        }

        int randomIdx = Random.Range(0, availableIndices.Count);
        selectedMapIndex = availableIndices[randomIdx];
        
        availableIndices.RemoveAt(randomIdx);

        string newAvailableStr = string.Join(",", availableIndices);
        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable() { { Constants.AvailableLevel3MapsProperty, newAvailableStr } });

        photonView.RPC(nameof(RpcStartRoulette), RpcTarget.AllBuffered, selectedMapIndex);
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
        
        if (rouletteAudio != null && rouletteAudio.clip != null) rouletteAudio.Play();

        if (AudioManager.Instance != null) 
        {
            AudioManager.Instance.TriggerDuckMusic(spinDuration);
        }

        if (mapCards == null || mapCards.Length == 0)
        {
            yield break;
        }

        int currentIdx = 0;
        int targetIdx = selectedMapIndex;
        int totalCards = mapCards.Length;
        int minLoops = 20;
        
        int targetOffset = (targetIdx - currentIdx + totalCards) % totalCards;
        int totalSteps = (minLoops * totalCards) + targetOffset;

        float delayPerStep = spinDuration / totalSteps;

        for (int i = 0; i < totalSteps; i++)
        {
            int highlightIdx = (currentIdx + i) % totalCards;
            HighlightCard(highlightIdx);

            yield return new WaitForSeconds(delayPerStep);
        }

        HighlightCard(selectedMapIndex, true);
        if (selectionText != null) selectionText.text = mapDisplayNames[selectedMapIndex] + "!";
        if (particleEffect != null) particleEffect.SetActive(true);

        yield return new WaitForSeconds(2f);

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(mapSceneNames[selectedMapIndex]);
        }
    }

    void HighlightCard(int index, bool isFinal = false)
    {
        for (int i = 0; i < mapCards.Length; i++)
        {
            if (mapCards[i] == null) continue;
            
            mapCards[i].color = Color.white;
            
            float scale = (i == index) ? (isFinal ? 1.2f : 1.1f) : 0.9f;
            mapCards[i].transform.localScale = Vector3.one * scale;

            Outline outline = mapCards[i].GetComponent<Outline>();
            if (outline == null) outline = mapCards[i].gameObject.AddComponent<Outline>();

            if (i == index)
            {
                outline.enabled = true;
                outline.effectColor = isFinal ? Color.green : Color.yellow;
                outline.effectDistance = new Vector2(10, -10);
            }
            else
            {
                outline.enabled = false;
            }
        }
    }
}
