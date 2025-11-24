using Unity.VisualScripting;
using UnityEngine;

public class CardSpawn : MonoBehaviour
{
    public GameObject worldPrefab;    // torreta
    public GameObject upgradePrefab;
    [SerializeField] public AudioClip upgradeSound;



    // Bot�n de carta de "torreta"
    public void OnBuildCardClicked()
    {
        var highlight = GetComponent<CardHighlight>(); 
        HandManager.Instance.SelectBuild(worldPrefab, highlight);
    }

    // Bot�n de carta de "mejora"
    public void OnUpgradeCardClicked()
    {
        var highlight = GetComponent<CardHighlight>();
        HandManager.Instance.SelectUpgrade(upgradePrefab, highlight, upgradeSound);
       
    }
}
