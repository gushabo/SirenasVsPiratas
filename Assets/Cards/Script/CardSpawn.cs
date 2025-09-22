using UnityEngine;

public class CardSpawn : MonoBehaviour
{
    public GameObject worldPrefab;    // torreta
    public GameObject upgradePrefab;

    // Botón de carta de "torreta"
    public void OnBuildCardClicked()
    {
        var highlight = GetComponent<CardHighlight>(); // asegúrate que está en el MISMO GO del botón
        HandManager.Instance.SelectBuild(worldPrefab, highlight);
    }

    // Botón de carta de "mejora"
    public void OnUpgradeCardClicked()
    {
        var highlight = GetComponent<CardHighlight>();
        HandManager.Instance.SelectUpgrade(upgradePrefab, highlight);
        Debug.Log("Seleccionada carta: Mejora");
    }
}
