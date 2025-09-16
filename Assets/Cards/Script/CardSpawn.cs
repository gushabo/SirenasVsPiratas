using UnityEngine;

public class CardSpawn : MonoBehaviour
{
    public GameObject worldPrefab;    // torreta

    public GameObject upgradePrefab;
  

    // Botón de carta de "torreta"
    public void OnBuildCardClicked()
    {
        CardPlacer.Instance.SetSelectedBuild(worldPrefab);
        Debug.Log("Seleccionada carta: Construcción");
    }

    // Botón de carta de "mejora"
    public void OnUpgradeCardClicked()
    {
        CardPlacer.Instance.SetSelectedUpgrade(upgradePrefab);
        Debug.Log("Seleccionada carta: Mejora");
    }
}
